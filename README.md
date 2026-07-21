# 历史粘贴板 (Swift 版)

Mac 菜单栏剪贴板管理工具 — 使用 Swift + SwiftUI 原生开发。

## 功能

- 🔄 自动记录文字和图片复制历史
- 🔍 实时搜索历史记录
- 📌 置顶常用内容（不受过期清理影响）
- 🗑️ 删除记录
- ⏱️ 可配置保留天数（1/3/5 天）
- 🚀 开机自动启动
- 🔒 所有数据仅存储在本地

## 在 Xcode 中打开并运行

### 方式一：通过 Terminal

```bash
cd /Users/HoujunMeng/Desktop/历史粘贴板/ClipboardManager-Swift
open Package.swift
```

### 方式二：通过 Xcode

1. 打开 Xcode
2. 菜单栏 File → Open...
3. 选择 `Package.swift` 文件
4. Xcode 会自动解析项目
5. 选择 **ClipboardManager** scheme
6. 按 ⌘R 运行

> **注意**：从 Xcode 运行时，程序会在 Dock 中出现瞬间后消失（这是正常的 — 程序使用了 `.accessory` 激活策略，仅在菜单栏显示）。

## 命令行构建

```bash
# Debug 构建并运行
make run

# Release 构建
make release

# 创建 .app 捆绑包（可双击运行）
make app
open .build/ClipboardManager.app

# 安装到 /Applications
make install

# 清理
make clean
```

## 项目结构

```
ClipboardManager-Swift/
├── Package.swift                    # SwiftPM 清单
├── Makefile                         # 构建脚本
├── Resources/
│   ├── Info.plist                   # App 捆绑包元数据
│   └── icon.png                     # 应用图标
├── README.md
└── Sources/ClipboardManager/
    ├── main.swift                   # 入口
    ├── AppDelegate.swift            # NSApplication 代理
    ├── Models/
    │   └── ClipboardItem.swift      # 数据模型
    ├── Services/
    │   ├── DatabaseService.swift    # SQLite CRUD
    │   ├── ClipboardMonitor.swift   # NSPasteboard 轮询
    │   └── SettingsManager.swift    # UserDefaults 设置
    ├── UI/
    │   ├── MenuBarManager.swift     # NSStatusBar + NSPopover
    │   ├── HistoryPanelView.swift   # 主面板 (SwiftUI)
    │   ├── ClipboardCardView.swift  # 卡片组件 (SwiftUI)
    │   └── SettingsView.swift       # 设置弹窗 (SwiftUI)
    └── Utils/
        └── Extensions.swift         # 颜色/尺寸/字符串扩展
```

## 技术栈

- **语言**：Swift 5.9
- **UI**：SwiftUI (面板) + AppKit (菜单栏/窗口管理)
- **数据库**：SQLite3 (C API)
- **剪贴板**：NSPasteboard
- **最低系统**：macOS 12 (Monterey)

## 数据存储

与 Python 版使用相同的路径和数据库 schema，数据可互通：

```
~/Library/Application Support/ClipboardManager/
├── history.db        # SQLite 数据库
├── images/           # 图片文件
└── settings.json     # Python 版使用（Swift 版使用 UserDefaults）
```

## 与 Python 版的对比

| 特性 | Python 版 | Swift 版 |
|------|-----------|----------|
| 框架 | PyQt6 | SwiftUI + AppKit |
| 体积 | ~100MB (含 Python) | ~10MB (原生) |
| 启动速度 | ~2 秒 | ~0.5 秒 |
| 内存 | ~80MB | ~30MB |
| 数据库 | 相同 Schema | 相同 Schema ✅ |
| 数据互通 | — | 与 Python 版兼容 ✅ |
