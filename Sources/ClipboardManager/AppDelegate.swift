import AppKit

/// 应用代理：初始化菜单栏和剪贴板监控
final class AppDelegate: NSObject, NSApplicationDelegate {
    private let menuBarManager = MenuBarManager()

    func applicationDidFinishLaunching(_ notification: Notification) {
        // 1. 启动时清理过期记录
        let retentionDays = SettingsManager.shared.retentionDays
        let cleaned = DatabaseService.shared.cleanup(retentionDays: retentionDays)
        if cleaned > 0 {
            print("[App] 启动清理：移除 \(cleaned) 条过期记录")
        }

        // 2. 应用开机启动设置
        SettingsManager.shared.applyLoginItem()

        // 3. 初始化菜单栏
        menuBarManager.setup()

        // 4. 启动剪贴板监控
        ClipboardMonitor.shared.start()

        print("[App] 历史粘贴板已启动")
    }

    func applicationWillTerminate(_ notification: Notification) {
        // 停止监控
        ClipboardMonitor.shared.stop()

        // 清理菜单栏
        menuBarManager.remove()

        print("[App] 历史粘贴板已退出")
    }
}
