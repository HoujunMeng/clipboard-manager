# 历史粘贴板

Mac 菜单栏剪贴板管理工具 — 自动记录文字和图片复制历史，随时搜索和复用。

## ✨ 功能

- 🔄 自动记录文字和图片复制历史
- 🔍 实时搜索历史记录
- 📌 置顶常用内容（不受过期清理影响）
- 🗑️ 删除记录
- ⏱️ 可配置保留天数（1 / 3 / 5 天）
- 🚀 开机自动启动
- 🔒 所有数据仅存储在本地，不上传任何服务器

## 📥 安装

1. 打开 [Releases](https://github.com/HoujunMeng/clipboard-manager/releases/latest) 页面
2. 下载最新的 `历史粘贴板-*.dmg`
3. 双击 DMG，将 **历史粘贴板** 拖入 `Applications` 文件夹
4. 打开终端，运行以下命令清除隔离标记：
   ```bash
   xattr -cr /Applications/历史粘贴板.app
   ```
5. 双击打开即可

> ⚠️ 如果打开时提示「已损坏」，是因为未签名应用被 macOS 拦截。运行第 4 步的命令即可解决。
>
> 💡 之后每次开机，历史粘贴板会自动出现在右上角菜单栏。

## 📋 系统要求

- macOS 12 (Monterey) 或更高版本
- Intel / Apple Silicon

## 🛠 开发

```bash
# 在 Xcode 中打开
open Package.swift

# 或命令行构建
make run          # Debug 构建并运行
make app          # 创建 .app 捆绑包
make dmg          # 创建 DMG 安装镜像
```

- **语言**：Swift 5.9 + SwiftUI + AppKit
- **数据库**：SQLite3
- **数据目录**：`~/Library/Application Support/ClipboardManager/`
