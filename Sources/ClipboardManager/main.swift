import AppKit

// MARK: — 应用入口

/// 历史粘贴板 — macOS 菜单栏剪贴板管理工具
/// 自动记录文字和图片复制历史，支持搜索、置顶、删除和再次粘贴。

let app = NSApplication.shared

// 设置为菜单栏辅助应用（不显示 Dock 图标）
app.setActivationPolicy(.accessory)

// 创建应用代理
let delegate = AppDelegate()
app.delegate = delegate

// 运行应用
app.run()
