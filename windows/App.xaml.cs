using System.Windows;
using Application = System.Windows.Application;

namespace ClipboardManager;

/// <summary>
/// 应用入口：初始化系统托盘和剪贴板监控。
/// 与 Mac 版 AppDelegate 生命周期对应。
/// 无主窗口 — 所有 UI 通过 TrayIconManager 触发。
/// </summary>
public partial class App : Application
{
    private UI.TrayIconManager? _trayManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 隐藏主窗口（WPF 惯例：Application 无 StartupUri 时不创建主窗口）
        // ShutdownMode = OnExplicitShutdown 防止窗口关闭时退出应用
        // 只有用户通过右键菜单「退出」才真正退出

        // 1. 启动时清理过期记录
        var retentionDays = Services.SettingsManager.Instance.RetentionDays;
        var cleaned = Services.DatabaseService.Instance.Cleanup(retentionDays);
        if (cleaned > 0)
            System.Diagnostics.Debug.WriteLine(
                $"[App] 启动清理：移除 {cleaned} 条过期记录");

        // 2. 应用开机启动设置
        Services.SettingsManager.Instance.ApplyLoginItem();

        // 3. 初始化系统托盘
        _trayManager = new UI.TrayIconManager();
        _trayManager.Setup();

        // 4. 启动剪贴板监控
        Services.ClipboardMonitor.Instance.Start();

        System.Diagnostics.Debug.WriteLine("[App] 历史粘贴板已启动");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 停止监控
        Services.ClipboardMonitor.Instance.Stop();

        // 清理托盘图标
        _trayManager?.Remove();

        System.Diagnostics.Debug.WriteLine("[App] 历史粘贴板已退出");
        base.OnExit(e);
    }
}
