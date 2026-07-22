# 历史粘贴板 (Windows)

Windows 系统托盘剪贴板管理工具 — 自动记录文字和图片复制历史，随时搜索和复用。

> 与 [Mac 版](https://github.com/HoujunMeng/clipboard-manager) 功能一致，数据库格式可互通。

## ✨ 功能

- 🔄 自动记录文字和图片复制历史
- 🔍 实时搜索历史记录
- 📌 置顶常用内容（不受过期清理影响）
- 🗑️ 删除记录
- ⏱️ 可配置保留天数（1 / 3 / 5 天）
- 🚀 开机自动启动
- 🔒 所有数据仅存储在本地，不上传任何服务器
- 🔗 与 Mac 版共享数据库格式（可互相拷贝使用）

## 🖱 使用方式

| 操作 | 效果 |
|------|------|
| **左键单击托盘图标** | 弹出/收起历史面板 |
| **右键单击托盘图标** | 显示菜单（设置 / 关于 / 退出） |
| **单击卡片** | 复制该条内容，面板自动关闭 |
| **拖拽选取文字** | 选中部分文字后 `Ctrl+C` 复制所选内容 |
| **点击 📌 图钉** | 置顶 / 取消置顶（置顶内容不受过期清理影响） |
| **点击 ✕ 按钮** | 删除该条记录 |
| **点击 ⚙ 齿轮** | 打开设置（保留天数、开机启动） |
| **搜索框** | 实时搜索历史记录 |

> 💡 卡片高度会根据文字长度自动调整，长文本不会溢出。

## 📥 安装

### 方式一：下载安装器（推荐）

1. 打开 [Releases](https://github.com/HoujunMeng/clipboard-manager/releases/latest) 页面
2. 下载 `ClipboardManager-Setup-*.exe`
3. 运行安装器，按向导完成安装
4. 安装完成后应用自动启动，任务栏右下角可见 📋 图标

### 方式二：免安装便携版

1. 下载 `ClipboardManager-portable-*.zip`
2. 解压到任意目录
3. 双击 `ClipboardManager.exe` 运行

> ⚠️ Windows SmartScreen 可能提示「已阻止不认识的应用程序」。点击「更多信息」→「仍然运行」即可。
> 如需彻底解决，需要 Authenticode 签名证书（付费）。

## 📋 系统要求

- Windows 10 1809+ / Windows 11
- 64 位 (x64)
- .NET 9 Desktop Runtime（安装器会自动安装，便携版需自行安装）

## 🗄 数据位置

| 内容 | 路径 |
|------|------|
| 数据库 | `%APPDATA%\ClipboardManager\history.db` |
| 图片 | `%APPDATA%\ClipboardManager\images\` |
| 配置 | `%APPDATA%\ClipboardManager\settings.json` |

> 💡 数据库格式与 Mac 版完全一致。将 Mac 的 `history.db` 拷贝到此路径即可无缝迁移数据。

## 🛠 开发

### 环境要求

- .NET 9 SDK
- Windows 10/11（WPF 需要 Windows 运行时）或通过 CI 构建

### 构建

```powershell
# 还原依赖
dotnet restore

# Debug 构建
dotnet build

# Release 发布（单文件夹）
dotnet publish -c Release -o publish

# 创建安装器（需要 Inno Setup 6）
# 1. 打开 installer/setup.iss
# 2. 点击 Compile
```

### 项目结构

```
ClipboardManager.Windows/
├── App.xaml / .cs               # 应用入口（隐藏主窗口、生命周期）
├── Models/
│   └── ClipboardItem.cs         # 数据模型
├── Services/
│   ├── ClipboardMonitor.cs      # 剪贴板 500ms 轮询 + SHA256 去重
│   ├── DatabaseService.cs       # SQLite CRUD + 自动清理
│   └── SettingsManager.cs       # JSON 配置 + 注册表开机自启
├── UI/
│   ├── MainWindow.xaml / .cs    # 弹出面板（全功能 UI）
│   ├── ClipboardCard.xaml / .cs # 卡片控件
│   ├── SettingsWindow.xaml / .cs# 设置窗口
│   └── TrayIconManager.cs      # 系统托盘 + Win32 API
├── Converters/
│   └── ValueConverters.cs       # XAML 绑定转换器
├── Utils/
│   └── DesignTokens.cs          # 设计令牌（颜色、尺寸）
├── Resources/
│   └── app.ico                  # 应用图标
├── installer/
│   └── setup.iss                # Inno Setup 安装脚本
└── docs/
    ├── 001-technical-spec.md    # 技术规范
    ├── 002-execution-plan.md    # 执行计划
    └── 003-requirements.md      # 需求文档
```

## 📄 许可证

MIT License — 详见仓库根目录。
