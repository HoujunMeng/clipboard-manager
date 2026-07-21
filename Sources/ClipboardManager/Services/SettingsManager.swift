import Foundation
import ServiceManagement

/// 用户设置管理器（UserDefaults 存储）
final class SettingsManager {
    static let shared = SettingsManager()

    private let defaults = UserDefaults.standard

    private enum Keys {
        static let retentionDays = "retention_days"
        static let launchAtLogin = "launch_at_login"
    }

    private init() {
        // 注册默认值
        defaults.register(defaults: [
            Keys.retentionDays: 3,
            Keys.launchAtLogin: true,
        ])
    }

    // MARK: — 保留天数（1/3/5 天）

    var retentionDays: Int {
        get { defaults.integer(forKey: Keys.retentionDays) }
        set { defaults.set(newValue, forKey: Keys.retentionDays) }
    }

    // MARK: — 开机启动

    var launchAtLogin: Bool {
        get { defaults.bool(forKey: Keys.launchAtLogin) }
        set {
            defaults.set(newValue, forKey: Keys.launchAtLogin)
            setLoginItem(enabled: newValue)
        }
    }

    /// 应用实际的开机启动设置
    func applyLoginItem() {
        setLoginItem(enabled: launchAtLogin)
    }

    private func setLoginItem(enabled: Bool) {
        if #available(macOS 13.0, *) {
            do {
                if enabled {
                    try SMAppService.mainApp.register()
                } else {
                    try SMAppService.mainApp.unregister()
                }
            } catch {
                print("[SettingsManager] SMAppService 操作失败: \(error.localizedDescription)")
            }
        } else {
            // macOS 12：使用旧版方式（需要 .app bundle 身份）
            // 降级到 LaunchAgent plist 方式
            if enabled {
                createLaunchAgent()
            } else {
                removeLaunchAgent()
            }
        }
    }

    // MARK: — LaunchAgent 后备方案（macOS 12）

    private func createLaunchAgent() {
        let label = "com.clipboardmanager.app"
        let plistPath = NSHomeDirectory() + "/Library/LaunchAgents/\(label).plist"
        let plistDir = (plistPath as NSString).deletingLastPathComponent

        try? FileManager.default.createDirectory(
            atPath: plistDir, withIntermediateDirectories: true)

        let executablePath = Bundle.main.executablePath
            ?? Bundle.main.bundlePath

        let plist: [String: Any] = [
            "Label": label,
            "ProgramArguments": [executablePath],
            "RunAtLoad": true,
            "KeepAlive": false,
        ]

        do {
            let data = try PropertyListSerialization.data(
                fromPropertyList: plist, format: .xml, options: 0)
            try data.write(to: URL(fileURLWithPath: plistPath))
            print("[SettingsManager] LaunchAgent 已创建: \(plistPath)")
        } catch {
            print("[SettingsManager] LaunchAgent 创建失败: \(error)")
        }
    }

    private func removeLaunchAgent() {
        let label = "com.clipboardmanager.app"
        let plistPath = NSHomeDirectory() + "/Library/LaunchAgents/\(label).plist"
        if FileManager.default.fileExists(atPath: plistPath) {
            try? FileManager.default.removeItem(atPath: plistPath)
            print("[SettingsManager] LaunchAgent 已移除")
        }
    }
}
