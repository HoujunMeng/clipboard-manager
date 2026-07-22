# 001 — 技术规范

**项目**：历史粘贴板 Windows 版
**版本**：1.0.0
**创建日期**：2026-07-22
**状态**：起草中

---

## 1. 技术栈

| 层级 | 技术 | 版本 |
|------|------|------|
| 语言 | C# | 13.0 |
| 运行时 | .NET | 9.0 |
| UI 框架 | WPF (Windows Presentation Foundation) | — |
| 数据库 | SQLite via `Microsoft.Data.Sqlite` | 9.x |
| ORM | 无（直接 SQL，与 Mac 版一致） | — |
| 配置存储 | JSON 文件 | — |
| 图像处理 | `System.Drawing.Common` | — |
| 安装器 | Inno Setup 6 | — |
| 目标系统 | Windows 10 1809+ / Windows 11 | — |

## 2. 项目结构

```
ClipboardManager.Windows/
├── ClipboardManager.Windows.csproj
├── App.xaml / App.xaml.cs             # 应用入口
├── Models/
│   └── ClipboardItem.cs               # 数据模型
├── Services/
│   ├── ClipboardMonitor.cs            # 剪贴板监控（500ms 轮询 + SHA256 去重）
│   ├── DatabaseService.cs             # SQLite CRUD
│   └── SettingsManager.cs             # JSON 配置读写
├── UI/
│   ├── MainWindow.xaml/.cs            # 弹出面板
│   ├── ClipboardCard.xaml/.cs         # 卡片控件
│   ├── SettingsWindow.xaml/.cs        # 设置窗口
│   └── TrayIconManager.cs            # 系统托盘逻辑
├── Converters/
│   └── ValueConverters.cs             # XAML 绑定转换器
├── Utils/
│   └── DesignTokens.cs               # 颜色、尺寸常量
└── Resources/
    └── app.ico                        # 应用图标
```

## 3. 数据库 Schema

与 Mac 版**完全一致**：

```sql
CREATE TABLE IF NOT EXISTS clipboard_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    content_type TEXT NOT NULL,          -- 'text' 或 'image'
    text_content TEXT,                    -- 文字内容
    image_path TEXT,                      -- 图片文件路径
    text_hash TEXT,                       -- SHA256 哈希（去重用）
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_pinned INTEGER DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_created_at ON clipboard_items(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_pinned ON clipboard_items(is_pinned);
```

## 4. 数据目录

| 内容 | 路径 |
|------|------|
| 根目录 | `%APPDATA%\ClipboardManager\` |
| 数据库 | `%APPDATA%\ClipboardManager\history.db` |
| 图片 | `%APPDATA%\ClipboardManager\images\` |
| 配置 | `%APPDATA%\ClipboardManager\settings.json` |

## 5. 配置 Schema (settings.json)

```json
{
  "retention_days": 3,
  "launch_at_login": true
}
```

## 6. 设计令牌

与 Mac 版完全一致，见 [DesignTokens.cs](../src/Utils/DesignTokens.cs)：

| 令牌 | 值 |
|------|-----|
| 面板宽度 | 380px |
| 面板高度 | 520px |
| 卡片圆角 | 6px |
| 卡片间距 | 6px |
| 卡片内边距 | 8px |
| 文字大小 | 13pt |
| 时间文字 | 12pt |
| 搜索框高度 | 36px |
| 底栏高度 | 20px |
| 缩略图最大宽度 | 330px |
| 缩略图最大高度 | 140px |

## 7. 剪贴板监控

| 参数 | 值 |
|------|-----|
| 轮询间隔 | 500ms |
| 文字哈希算法 | SHA256 of UTF-8 bytes |
| 图片哈希算法 | SHA256 of 32×32 PNG thumbnail |
| 自动清理频率 | 每 50 条保存后 |
| 图片磁盘格式 | 粘贴板 PNG → 存为 PNG；文件复制 → 保持原格式 |

## 8. 去重策略

1. 每次检测到剪贴板变化时，检查 `changeCount`
2. 计算 SHA256 哈希
3. 与上一条记录的哈希比对（文字和图片分别追踪）
4. 相同 → 跳过；不同 → 保存
5. 应用自身写入剪贴板时（复制回用户应用），通过 `ignoreOnce` 标志跳过一次检测

## 9. Windows 平台特殊处理

| 项目 | 实现 |
|------|------|
| 主窗口隐藏 | `Window.Visibility = Hidden`, `ShowInTaskbar = false` |
| 托盘图标 | `System.Windows.Forms.NotifyIcon` |
| 开机自启 | 注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| 面板弹出定位 | 根据任务栏位置动态计算 |
| 面板失焦关闭 | 监听 `Window.Deactivated` 事件 |
| 图片格式 | Bitmap/DIB/PNG 分级尝试 |
| 文件复制检测 | `Clipboard.ContainsFileDropList()` |
| App 图标 | 多尺寸 `.ico`（16/32/48/256） |

---

## 版本历史

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-07-22 | 1.0.0 | 初稿 |
