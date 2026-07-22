using System.Windows;
using ClipboardManager.Services;

namespace ClipboardManager.UI;

/// <summary>
/// 设置弹窗。
/// 与 Mac 版 SettingsView 对应。
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly int[] _dayOptions = [1, 3, 5];
    private int _initialDays;
    private bool _initialLaunch;

    public SettingsWindow()
    {
        InitializeComponent();

        // 加载当前设置
        var settings = SettingsManager.Instance;
        _initialDays = settings.RetentionDays;
        _initialLaunch = settings.LaunchAtLogin;

        // 设置保留天数单选按钮
        var dayIndex = Array.IndexOf(_dayOptions, _initialDays);
        if (dayIndex < 0) dayIndex = 1; // 默认 3 天

        switch (dayIndex)
        {
            case 0: Day1.IsChecked = true; break;
            case 1: Day3.IsChecked = true; break;
            case 2: Day5.IsChecked = true; break;
        }

        // 设置开机启动开关
        LaunchToggle.IsChecked = _initialLaunch;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var settings = SettingsManager.Instance;

        // 读取保留天数选择
        int newDays;
        if (Day1.IsChecked == true) newDays = 1;
        else if (Day5.IsChecked == true) newDays = 5;
        else newDays = 3; // 默认

        int oldDays = settings.RetentionDays;
        settings.RetentionDays = newDays;

        // 保留天数变更时触发清理
        if (newDays != oldDays)
        {
            DatabaseService.Instance.Cleanup(newDays);
        }

        // 开机启动
        settings.LaunchAtLogin = LaunchToggle.IsChecked == true;

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
