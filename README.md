# 历史粘贴板

[![macOS](https://img.shields.io/badge/macOS-12%2B-blue?logo=apple)](https://github.com/HoujunMeng/clipboard-manager/releases/latest)
[![Windows](https://img.shields.io/badge/Windows-10%2B-blue?logo=windows)](https://github.com/HoujunMeng/clipboard-manager/releases/latest)
[![Swift](https://img.shields.io/badge/Swift-5.9-orange?logo=swift)](.)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple?logo=dotnet)](.)

跨平台系统托盘剪贴板管理工具 — 自动记录文字和图片复制历史，随时搜索和复用。

> 💡 Mac 版和 Windows 版使用相同数据库格式，可互相拷贝 `history.db` 迁移数据。

## ✨ 功能

- 🔄 自动记录文字和图片复制历史
- 🔍 实时搜索历史记录
- 📌 置顶常用内容（不受过期清理影响）
- 🗑️ 删除记录
- ⏱️ 可配置保留天数（1 / 3 / 5 天）
- 🚀 开机自动启动
- 🔒 所有数据仅存储在本地，不上传任何服务器
- 🔗 跨平台数据库互通

## 🖱 使用方式

Mac 版：点击**右上角**菜单栏图标；Windows 版：点击**右下角**系统托盘图标。

| 操作 | 效果 |
|------|------|
| **单击卡片** | 复制该条内容，面板自动关闭 |
| **拖拽选取文字** | 选中部分文字后 `⌘C` / `Ctrl+C` 复制所选内容 |
| **点击 📌 图钉** | 置顶 / 取消置顶（置顶内容不受过期清理影响） |
| **点击 ✕ 按钮** | 删除该条记录 |
| **点击 ⚙ 齿轮** | 打开设置（保留天数、开机启动） |
| **搜索框** | 实时搜索历史记录 |
| **右键托盘图标** | 设置 / 关于 / 退出 |

## 📥 安装

### macOS

1. 打开 [Releases](https://github.com/HoujunMeng/clipboard-manager/releases/latest) 页面
2. 下载最新的 `历史粘贴板-*.dmg`
3. 双击 DMG，将 **ClipboardManager** 拖入 `Applications` 文件夹
4. 打开终端，运行以下命令清除隔离标记：
   ```bash
   xattr -cr /Applications/ClipboardManager.app
   ```
5. 双击打开即可（Finder 显示为「历史粘贴板」）

> ⚠️ 如果打开时提示「已损坏」，运行第 4 步的命令即可解决。

### Windows

1. 打开 [Releases](https://github.com/HoujunMeng/clipboard-manager/releases/latest) 页面
2. 下载 `ClipboardManager-Windows-portable.zip`

   > ⚠️ **关键步骤：下载后先右键 ZIP → 属性 → 勾选「解除锁定」→ 确定**，再解压。否则 Windows 会因为文件来自互联网而拦截运行。
   >
   > ![解除锁定示意](https://github.com/user-attachments/assets/placeholder)

3. 将 ZIP 解压到任意目录（如 `C:\Users\你的用户名\AppData\Local\ClipboardManager\`）
4. 双击 `ClipboardManager.exe` 运行
5. 首次运行时可在设置中开启「开机自动启动」

> 💡 **必备环境**：[.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)（选择「Run desktop apps」下的 x64 下载）。
>
> 💡 **如果系统仍然拦截**：点击「更多信息」→「仍然运行」。随着更多用户下载，SmartScreen 会自动降低拦截频率。

## 📋 系统要求

| 平台 | 要求 |
|------|------|
| **macOS** | macOS 12 (Monterey) 或更高版本 · Intel / Apple Silicon |
| **Windows** | Windows 10 1809+ / Windows 11 · 64-bit · .NET 9 Desktop Runtime |

## 🗄 数据位置

| 内容 | macOS | Windows |
|------|-------|---------|
| 数据库 | `~/Library/Application Support/ClipboardManager/history.db` | `%APPDATA%\ClipboardManager\history.db` |
| 图片 | `~/Library/Application Support/ClipboardManager/images/` | `%APPDATA%\ClipboardManager\images\` |
| 配置 | UserDefaults | `%APPDATA%\ClipboardManager\settings.json` |

> 💡 数据库 schema 完全一致，可跨平台拷贝使用。

## 🛠 开发

### macOS 版

```bash
# 在 Xcode 中打开
open Package.swift

# 或命令行构建
make run          # Debug 构建并运行
make app          # 创建 .app 捆绑包
make dmg          # 创建 DMG 安装镜像
```

- **语言**：Swift 5.9 + SwiftUI + AppKit
- **数据库**：SQLite3 (C API)
- **源码**：`Sources/ClipboardManager/`

### Windows 版

```powershell
# 还原依赖
dotnet restore windows/ClipboardManager.Windows.csproj

# 构建
dotnet build windows/ClipboardManager.Windows.csproj

# 发布
dotnet publish windows/ClipboardManager.Windows.csproj -c Release -o windows-publish
```

- **语言**：C# 13 + WPF (.NET 9)
- **数据库**：Microsoft.Data.Sqlite
- **源码**：`windows/`
- **文档**：[技术规范](windows/docs/001-technical-spec.md) · [执行计划](windows/docs/002-execution-plan.md) · [需求文档](windows/docs/003-requirements.md)

## 📄 许可证

MIT License
