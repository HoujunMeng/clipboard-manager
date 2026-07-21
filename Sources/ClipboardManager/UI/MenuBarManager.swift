import AppKit
import SwiftUI

/// 菜单栏管理器：NSStatusBar 图标 + NSPopover 面板弹出/收起
final class MenuBarManager: NSObject, ObservableObject {
    private var statusItem: NSStatusItem!
    private var popover: NSPopover!
    private let panelVM = HistoryPanelViewModel()

    /// 去抖：macOS 单次点击会触发两次 Trigger
    private var lastTriggerTime: TimeInterval = 0
    private let debounceInterval: TimeInterval = 0.3

    // MARK: — 初始化

    func setup() {
        // 创建菜单栏图标
        statusItem = NSStatusBar.system.statusItem(
            withLength: NSStatusItem.variableLength)

        if let button = statusItem.button {
            button.image = createMenuBarIcon()
            button.image?.isTemplate = true // 自动适配明暗菜单栏
            button.toolTip = "历史粘贴板"
            button.target = self
            button.action = #selector(togglePanel)
            // 允许右键
            button.sendAction(on: [.leftMouseUp, .rightMouseUp])
        }

        // 复制内容后自动关闭面板
        panelVM.closePanel = { [weak self] in
            self?.closePanel()
        }

        // 创建弹出面板
        popover = NSPopover()
        popover.contentSize = NSSize(
            width: DesignMetrics.panelWidth,
            height: DesignMetrics.panelHeight)
        popover.behavior = .transient // 点击外部自动关闭
        popover.animates = false
        popover.contentViewController = NSHostingController(
            rootView: HistoryPanelView(viewModel: panelVM)
                .environmentObject(self)
        )

        print("[MenuBarManager] 菜单栏图标已创建")
    }

    func remove() {
        popover?.close()
        if let item = statusItem {
            NSStatusBar.system.removeStatusItem(item)
        }
    }

    // MARK: — 图标生成

    private func createMenuBarIcon() -> NSImage {
        // 使用 SF Symbol：剪贴板 + 文档（类似 Word 粘贴图标）
        if let image = NSImage(
            systemSymbolName: "doc.on.clipboard",
            accessibilityDescription: "历史粘贴板"
        ) {
            let config = NSImage.SymbolConfiguration(
                pointSize: 18,
                weight: .medium
            )
            if let configured = image.withSymbolConfiguration(config) {
                configured.isTemplate = true
                return configured
            }
            image.isTemplate = true
            return image
        }
        // 降级：返回空白图标
        return NSImage(size: NSSize(width: 22, height: 22))
    }

    // MARK: — 面板切换

    @objc private func togglePanel() {
        // 检测右键 → 显示菜单
        guard let event = NSApp.currentEvent else { return }

        if event.type == .rightMouseUp {
            showContextMenu()
            return
        }

        // 去抖（macOS 单次点击可能触发两次 Trigger）
        let now = ProcessInfo.processInfo.systemUptime
        if now - lastTriggerTime < debounceInterval {
            return
        }
        lastTriggerTime = now

        if popover.isShown {
            closePanel()
        } else {
            showPanel()
        }
    }

    func showPanel() {
        guard let button = statusItem?.button else { return }
        panelVM.refresh()
        popover.show(relativeTo: button.bounds, of: button, preferredEdge: .minY)
        // 确保面板获得焦点
        popover.contentViewController?.view.window?.makeKey()
    }

    func closePanel() {
        popover.close()
    }

    // MARK: — 右键菜单

    private func showContextMenu() {
        let menu = NSMenu()

        let settingsItem = NSMenuItem(
            title: "设置...",
            action: #selector(openSettings),
            keyEquivalent: ","
        )
        settingsItem.target = self
        menu.addItem(settingsItem)

        menu.addItem(.separator())

        let aboutItem = NSMenuItem(
            title: "关于",
            action: #selector(showAbout),
            keyEquivalent: ""
        )
        aboutItem.target = self
        menu.addItem(aboutItem)

        menu.addItem(.separator())

        let quitItem = NSMenuItem(
            title: "退出",
            action: #selector(quitApp),
            keyEquivalent: "q"
        )
        quitItem.target = self
        menu.addItem(quitItem)

        // 在菜单栏图标处弹出
        if let button = statusItem?.button {
            menu.popUp(
                positioning: nil,
                at: NSPoint(x: 0, y: button.bounds.minY),
                in: button
            )
        }
    }

    @objc private func openSettings() {
        closePanel()
        // 延迟打开设置，确保 popover 已关闭
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.15) { [weak self] in
            self?.panelVM.showSettings()
        }
    }

    @objc private func showAbout() {
        let alert = NSAlert()
        alert.messageText = "历史粘贴板 v1.0"
        alert.informativeText = """
        一款简洁的 Mac 菜单栏剪贴板管理工具。

        自动记录文字和图片的复制历史，支持搜索、置顶和再次粘贴。

        数据仅存储在本地，保障隐私安全。
        """
        alert.alertStyle = .informational
        alert.addButton(withTitle: "确定")
        alert.runModal()
    }

    @objc private func quitApp() {
        closePanel()
        ClipboardMonitor.shared.stop()
        NSApp.terminate(nil)
    }
}
