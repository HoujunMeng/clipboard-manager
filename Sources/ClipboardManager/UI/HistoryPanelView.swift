import SwiftUI
import AppKit

// MARK: — ViewModel

/// 历史面板的视图模型
final class HistoryPanelViewModel: ObservableObject {
    @Published var items: [ClipboardItem] = []
    @Published var searchText: String = ""
    @Published var totalCount: Int = 0
    @Published var retentionDays: Int = 3

    private var settingsWindow: NSWindow?
    /// 强引用 WindowDelegate，防止被立即释放（NSWindow.delegate 是 weak）
    private var windowDelegate: NSObject?

    /// 复制内容后关闭面板的回调（由 MenuBarManager 设置）
    var closePanel: (() -> Void)?

    init() {
        // 监听剪贴板新增通知
        NotificationCenter.default.addObserver(
            self,
            selector: #selector(onNewItem),
            name: ClipboardMonitor.newItemNotification,
            object: nil
        )
    }

    deinit {
        NotificationCenter.default.removeObserver(self)
    }

    // MARK: — 数据加载

    func load(search: String = "") {
        items = DatabaseService.shared.getItems(searchQuery: search)
        totalCount = DatabaseService.shared.getTotalCount()
        retentionDays = SettingsManager.shared.retentionDays
    }

    func refresh() {
        load(search: searchText)
    }

    @objc private func onNewItem() {
        DispatchQueue.main.async { [weak self] in
            self?.refresh()
        }
    }

    // MARK: — 操作

    func copyItem(id: Int) {
        guard let item = DatabaseService.shared.getItem(id: id) else { return }

        let pasteboard = NSPasteboard.general
        ClipboardMonitor.shared.ignoreOnce()
        pasteboard.clearContents()

        switch item.contentType {
        case .text:
            if let text = item.textContent {
                pasteboard.setString(text, forType: .string)
            }
        case .image:
            if let imagePath = item.imagePath,
               FileManager.default.fileExists(atPath: imagePath),
               let image = NSImage(contentsOfFile: imagePath) {
                pasteboard.writeObjects([image])
            }
        }

        // 复制后关闭面板（与 Python 版行为一致）
        // 延迟到下一轮事件循环，避免在事件处理中销毁视图
        DispatchQueue.main.async { [weak self] in
            self?.closePanel?()
        }
    }

    func togglePin(id: Int) {
        _ = DatabaseService.shared.togglePin(itemId: id)
        refresh()
    }

    func deleteItem(id: Int) {
        DatabaseService.shared.deleteItem(itemId: id)
        refresh()
    }

    // MARK: — 设置窗口

    func showSettings() {
        if let existingWindow = settingsWindow {
            existingWindow.makeKeyAndOrderFront(nil)
            NSApp.activate(ignoringOtherApps: true)
            return
        }

        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 320, height: 200),
            styleMask: [.titled, .closable],
            backing: .buffered,
            defer: false
        )
        window.title = "设置"
        window.center()
        window.isReleasedWhenClosed = false
        window.contentView = NSHostingView(
            rootView: SettingsView()
        )

        // 关闭时释放引用
        let delegate = WindowDelegate { [weak self] in
            self?.settingsWindow = nil
            self?.windowDelegate = nil
        }
        self.windowDelegate = delegate
        window.delegate = delegate

        settingsWindow = window
        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }
}

// MARK: — 窗口代理

private final class WindowDelegate: NSObject, NSWindowDelegate {
    private let onClose: () -> Void

    init(onClose: @escaping () -> Void) {
        self.onClose = onClose
    }

    func windowWillClose(_ notification: Notification) {
        onClose()
    }
}

// MARK: — 主面板视图

struct HistoryPanelView: View {
    @ObservedObject var viewModel: HistoryPanelViewModel
    @State private var gearHovered = false

    var body: some View {
        VStack(spacing: 0) {
            // 搜索框
            searchBar
                .padding(.horizontal, 12)
                .padding(.top, 12)
                .padding(.bottom, 8)

            // 卡片列表
            cardList

            Divider()

            // 底部状态栏
            statusBar
                .padding(.horizontal, 12)
                .padding(.vertical, 4)
        }
        .frame(width: DesignMetrics.panelWidth, height: DesignMetrics.panelHeight)
        .background(Color.panelBg)
        .onAppear {
            viewModel.refresh()
        }
    }

    // MARK: — 搜索框

    private var searchBar: some View {
        HStack(spacing: 0) {
            Image(systemName: "magnifyingglass")
                .foregroundColor(.statusText)
                .font(.system(size: 13))
                .padding(.leading, 10)

            TextField("搜索复制历史...", text: $viewModel.searchText)
                .textFieldStyle(.plain)
                .font(.system(size: 14))
                .padding(.vertical, 8)
                .padding(.horizontal, 6)
                .onChange(of: viewModel.searchText) { newValue in
                    viewModel.load(search: newValue.trimmingCharacters(in: .whitespaces))
                }

            if !viewModel.searchText.isEmpty {
                Button(action: {
                    viewModel.searchText = ""
                    viewModel.load(search: "")
                }) {
                    Image(systemName: "xmark.circle.fill")
                        .foregroundColor(.statusText)
                        .font(.system(size: 13))
                }
                .buttonStyle(.plain)
                .padding(.trailing, 8)
            }
        }
        .frame(height: DesignMetrics.searchHeight)
        .background(Color.white)
        .overlay(
            RoundedRectangle(cornerRadius: 8)
                .stroke(Color(red: 0.816, green: 0.816, blue: 0.816), lineWidth: 1)
        )
        .clipShape(RoundedRectangle(cornerRadius: 8))
    }

    // MARK: — 卡片列表

    private var cardList: some View {
        ScrollView {
            LazyVStack(spacing: 6) {
                if viewModel.items.isEmpty {
                    emptyState
                } else {
                    ForEach(viewModel.items) { item in
                        ClipboardCardView(
                            item: item,
                            onCopy: { id in
                                viewModel.copyItem(id: id)
                                // 复制后自动关闭面板
                                DispatchQueue.main.async {
                                    // 通过通知关闭面板
                                }
                            },
                            onPin: { id in viewModel.togglePin(id: id) },
                            onDelete: { id in viewModel.deleteItem(id: id) }
                        )
                    }
                }
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 4)
        }
    }

    private var emptyState: some View {
        VStack(spacing: 8) {
            Image(systemName: viewModel.searchText.isEmpty
                  ? "doc.on.clipboard" : "magnifyingglass")
                .font(.system(size: 32))
                .foregroundColor(.emptyText)

            Text(viewModel.searchText.isEmpty
                 ? "暂无复制记录" : "无匹配记录")
                .font(.system(size: 14))
                .foregroundColor(.emptyText)
        }
        .frame(maxWidth: .infinity)
        .padding(.vertical, 60)
    }

    // MARK: — 状态栏

    private var statusBar: some View {
        HStack(spacing: 4) {
            Text("共 \(viewModel.totalCount) 条记录 · 保留 \(viewModel.retentionDays) 天")
                .font(.system(size: 11))
                .foregroundColor(.statusText)

            Spacer()

            // 齿轮设置按钮
            Button(action: { viewModel.showSettings() }) {
                Image(systemName: "gearshape.fill")
                    .font(.system(size: 15))
                    .foregroundColor(.statusText)
                    .opacity(gearHovered ? 0.7 : 1.0)
            }
            .buttonStyle(.plain)
            .help("设置")
            .onHover { hovering in
                gearHovered = hovering
            }
        }
        .frame(height: 24)
    }
}
