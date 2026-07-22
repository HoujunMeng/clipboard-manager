using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MenuItem = System.Windows.Forms.MenuItem;
using Timer = System.Windows.Forms.Timer;

namespace ClipboardManager.UI;

/// <summary>
/// 系统托盘管理器。
/// 与 Mac 版 MenuBarManager 对应：
/// - NSStatusItem → NotifyIcon
/// - NSPopover → 弹出 Window
/// - 右键菜单 → ContextMenu
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private MainWindow? _panelWindow;
    private bool _panelClosing;

    /// <summary>去抖：防止连续点击误触发</summary>
    private DateTime _lastTriggerTime = DateTime.MinValue;
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(300);

    // ── 初始化 ──────────────────────────────────

    public void Setup()
    {
        // 创建系统托盘图标
        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "历史粘贴板",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        // 左键点击 → 切换面板
        _notifyIcon.MouseClick += OnTrayIconClick;

        System.Diagnostics.Debug.WriteLine("[TrayIconManager] 系统托盘图标已创建");
    }

    public void Remove()
    {
        HidePanel();
        _panelWindow?.Close();
        _panelWindow = null;

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }

    public void Dispose() => Remove();

    // ── 图标 ────────────────────────────────────

    private static Icon LoadIcon()
    {
        // 1. 尝试从 Resources 加载 .ico 文件
        try
        {
            var icoPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (System.IO.File.Exists(icoPath))
                return new Icon(icoPath);

            // 也尝试应用程序同级目录
            var altPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (System.IO.File.Exists(altPath))
                return new Icon(altPath);
        }
        catch
        {
            // 忽略加载错误，使用备用方案
        }

        // 2. 备用：动态绘制剪贴板图标
        return CreateFallbackIcon();
    }

    /// <summary>
    /// 动态绘制一个剪贴板形状的图标（备用方案，直到 .ico 文件到位）。
    /// 在浅色任务栏上显示深色图标，深色任务栏上显示浅色图标。
    /// </summary>
    private static Icon CreateFallbackIcon()
    {
        // 创建 64×64 的高分辨率图标（系统会缩放到合适大小）
        const int size = 64;
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAliasing;

        // 透明背景
        g.Clear(Color.Transparent);

        // 简单剪贴板形状：圆角矩形 + 顶部夹子
        using var pen = new Pen(Color.FromArgb(60, 60, 60), 3.5f);
        var rect = new Rectangle(12, 16, 40, 40);
        const int radius = 6;

        // 圆角矩形（剪贴板主体）
        using (var path = CreateRoundedRect(rect, radius))
        {
            g.DrawPath(pen, path);
        }

        // 剪贴板顶部夹子
        g.DrawLine(pen, 22, 16, 42, 16);
        g.FillEllipse(Brushes.Black, 29, 10, 6, 6);

        pen.Dispose();
        var handle = bitmap.GetHicon();
        return Icon.FromHandle(handle);
    }

    private static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    // ── 右键菜单 ────────────────────────────────

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var settingsItem = new ToolStripMenuItem("设置...", null, OnOpenSettings);
        settingsItem.ShortcutKeyDisplayString = "Ctrl+,";
        menu.Items.Add(settingsItem);

        menu.Items.Add(new ToolStripSeparator());

        var aboutItem = new ToolStripMenuItem("关于", null, OnShowAbout);
        menu.Items.Add(aboutItem);

        menu.Items.Add(new ToolStripSeparator());

        var quitItem = new ToolStripMenuItem("退出", null, OnQuitApp);
        quitItem.ShortcutKeyDisplayString = "Ctrl+Q";
        menu.Items.Add(quitItem);

        return menu;
    }

    // ── 面板显示/隐藏 ───────────────────────────

    private void OnTrayIconClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
            return; // 右键菜单由 ContextMenuStrip 处理

        // 去抖（Windows 同样可能出现双击事件）
        var now = DateTime.UtcNow;
        if (now - _lastTriggerTime < DebounceInterval)
            return;
        _lastTriggerTime = now;

        TogglePanel();
    }

    private void TogglePanel()
    {
        if (_panelWindow != null && _panelWindow.IsVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    public void ShowPanel()
    {
        if (_panelWindow == null)
        {
            _panelWindow = new MainWindow();
            _panelWindow.Deactivated += OnPanelDeactivated;
            _panelWindow.PreviewKeyDown += OnPanelKeyDown;
        }

        // // 刷新数据（Phase 4 实现）
        // _panelWindow.RefreshData();

        PositionPanel();
        _panelWindow.Show();
        _panelWindow.Activate();
    }

    public void HidePanel()
    {
        if (_panelWindow != null)
        {
            _panelClosing = true;
            _panelWindow.Hide();
            _panelClosing = false;
        }
        // 注意：不要在这里 Dispose 窗口，复用以保留状态
    }

    /// <summary>
    /// 面板失焦时自动关闭（与 Mac 版 popover.behavior = .transient 行为一致）
    /// </summary>
    private void OnPanelDeactivated(object? sender, EventArgs e)
    {
        // 防止在关闭过程中递归触发
        if (!_panelClosing && _panelWindow != null && _panelWindow.IsVisible)
        {
            HidePanel();
        }
    }

    private void OnPanelKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            HidePanel();
            e.Handled = true;
        }
    }

    // ── 面板定位 ────────────────────────────────

    /// <summary>
    /// 计算面板弹出位置：对齐任务栏图标上方。
    /// 通过 Win32 API 获取任务栏位置和托盘图标位置。
    /// </summary>
    private void PositionPanel()
    {
        if (_panelWindow == null || _notifyIcon == null)
            return;

        // 获取系统托盘图标的屏幕坐标
        var iconRect = GetTrayIconRect();
        if (iconRect == Rectangle.Empty)
        {
            // 降级：显示在屏幕右下角
            var screen = Screen.PrimaryScreen;
            if (screen == null) return;
            var workArea = screen.WorkingArea;
            _panelWindow.Left = workArea.Right - _panelWindow.Width - 10;
            _panelWindow.Top = workArea.Bottom - _panelWindow.Height - 10;
            return;
        }

        // 获取任务栏位置，判断面板弹出方向
        var taskbarRect = GetTaskbarRect();
        double panelLeft = iconRect.Left + (iconRect.Width / 2.0) - (_panelWindow.Width / 2.0);
        double panelTop;

        if (taskbarRect.Top > 0)
        {
            // 任务栏在屏幕底部 → 面板向上弹出
            panelTop = taskbarRect.Top - _panelWindow.Height - 5;
        }
        else if (taskbarRect.Left > 0)
        {
            // 任务栏在屏幕右侧
            panelTop = taskbarRect.Bottom - _panelWindow.Height - 5;
        }
        else if (taskbarRect.Bottom < Screen.PrimaryScreen!.Bounds.Height)
        {
            // 任务栏在屏幕顶部
            panelTop = taskbarRect.Bottom + 5;
        }
        else
        {
            // 任务栏在屏幕左侧
            panelTop = taskbarRect.Bottom - _panelWindow.Height - 5;
        }

        // 防止面板超出屏幕边界
        var screen = Screen.FromPoint(
            new System.Drawing.Point((int)panelLeft, (int)panelTop));
        if (screen == null) return;

        var bounds = screen.WorkingArea;
        if (panelLeft < bounds.Left) panelLeft = bounds.Left + 5;
        if (panelLeft + _panelWindow.Width > bounds.Right)
            panelLeft = bounds.Right - _panelWindow.Width - 5;
        if (panelTop < bounds.Top) panelTop = bounds.Top + 5;

        _panelWindow.Left = panelLeft;
        _panelWindow.Top = panelTop;
    }

    // ── Win32 API ────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter,
        string lpClassName, string? lpWindowName);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    /// <summary>
    /// 通过 Shell_TrayWnd → TrayNotifyWnd → ToolbarWindow32 定位托盘图标。
    /// 注意：这个方法返回的是通知区域的位置，而非精确的图标位置。
    /// 精确图标位置需要 UI Automation / TB_GETRECT (Windows 7+)。
    /// </summary>
    private Rectangle GetTrayIconRect()
    {
        // 遍历任务栏层级找到通知区域
        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero) return Rectangle.Empty;

        var trayNotify = FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
        if (trayNotify == IntPtr.Zero) return Rectangle.Empty;

        // 尝试多种方式获取托盘通知区域位置
        // 方式 1：SysPager → ToolbarWindow32
        var sysPager = FindWindowEx(trayNotify, IntPtr.Zero, "SysPager", null);
        if (sysPager != IntPtr.Zero)
        {
            var toolbar = FindWindowEx(sysPager, IntPtr.Zero, "ToolbarWindow32", null);
            if (toolbar != IntPtr.Zero)
            {
                if (GetWindowRect(toolbar, out var rect))
                    return new Rectangle(rect.Left, rect.Top,
                        rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
        }

        // 方式 2：直接查找 ToolbarWindow32（Win11 简化的任务栏）
        var toolbar2 = FindWindowEx(trayNotify, IntPtr.Zero, "ToolbarWindow32", null);
        if (toolbar2 != IntPtr.Zero)
        {
            if (GetWindowRect(toolbar2, out var rect))
                return new Rectangle(rect.Left, rect.Top,
                    rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        // 最终降级：使用 TrayNotifyWnd 的位置
        if (GetWindowRect(trayNotify, out var trayRect))
            return new Rectangle(trayRect.Left, trayRect.Top,
                trayRect.Right - trayRect.Left, trayRect.Bottom - trayRect.Top);

        return Rectangle.Empty;
    }

    /// <summary>
    /// 获取任务栏矩形区域，用于判断任务栏位置（顶部/底部/左侧/右侧）
    /// </summary>
    private static Rectangle GetTaskbarRect()
    {
        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar != IntPtr.Zero && GetWindowRect(taskbar, out var rect))
            return new Rectangle(rect.Left, rect.Top,
                rect.Right - rect.Left, rect.Bottom - rect.Top);
        return Rectangle.Empty;
    }

    // ── 菜单回调 ────────────────────────────────

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        HidePanel();
        // 延迟打开设置窗口，确保面板已关闭
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() =>
            {
                var settingsWin = new SettingsWindow();
                settingsWin.ShowDialog();
            }));
    }

    private void OnShowAbout(object? sender, EventArgs e)
    {
        System.Windows.MessageBox.Show(
            "历史粘贴板 v1.0\n\n" +
            "一款简洁的 Windows 剪贴板管理工具。\n\n" +
            "自动记录文字和图片的复制历史，支持搜索、置顶和再次粘贴。\n\n" +
            "数据仅存储在本地，保障隐私安全。",
            "关于 历史粘贴板",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnQuitApp(object? sender, EventArgs e)
    {
        HidePanel();
        Services.ClipboardMonitor.Instance.Stop();
        Application.Current.Shutdown();
    }
}
