using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace ClipboardManager.Services;

/// <summary>
/// 用户设置管理器（JSON 文件存储）。
/// 与 Mac 版 SettingsManager 功能一致：
/// - 保留天数（1/3/5 天）
/// - 开机自动启动（注册表 Run 键）
/// </summary>
public sealed class SettingsManager
{
    public static SettingsManager Instance { get; } = new();

    private readonly string _settingsPath;
    private SettingsData _data;

    // ── JSON 模型 ────────────────────────────────

    private sealed class SettingsData
    {
        public int RetentionDays { get; set; } = 3;
        public bool LaunchAtLogin { get; set; } = true;
    }

    // ── 初始化 ──────────────────────────────────

    private SettingsManager() : this(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipboardManager"))
    { }

    /// <summary>测试用构造函数：指定数据目录</summary>
    internal SettingsManager(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        _settingsPath = Path.Combine(dataDir, "settings.json");

        _data = Load();
    }

    private SettingsData Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var data = JsonSerializer.Deserialize<SettingsData>(json);
                if (data != null)
                    return data;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[SettingsManager] 加载设置失败: {ex.Message}");
        }
        return new SettingsData(); // 默认值
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[SettingsManager] 保存设置失败: {ex.Message}");
        }
    }

    // ── 属性 ─────────────────────────────────────

    /// <summary>保留天数（1/3/5）</summary>
    public int RetentionDays
    {
        get => _data.RetentionDays;
        set
        {
            if (_data.RetentionDays != value)
            {
                _data.RetentionDays = value;
                Save();
            }
        }
    }

    /// <summary>开机自动启动</summary>
    public bool LaunchAtLogin
    {
        get => _data.LaunchAtLogin;
        set
        {
            if (_data.LaunchAtLogin != value)
            {
                _data.LaunchAtLogin = value;
                Save();
                SetLoginItem(value);
            }
        }
    }

    /// <summary>应用开机启动设置（启动时调用）</summary>
    public void ApplyLoginItem()
    {
        SetLoginItem(_data.LaunchAtLogin);
    }

    // ── 注册表操作 ───────────────────────────────

    private const string RunKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppValueName = "ClipboardManager";

    private static void SetLoginItem(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                RunKeyPath, writable: true);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath
                    ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    "ClipboardManager.exe");
                key.SetValue(AppValueName, $"\"{exePath}\"");
                System.Diagnostics.Debug.WriteLine(
                    "[SettingsManager] 已添加到开机启动");
            }
            else
            {
                key.DeleteValue(AppValueName, throwOnMissingValue: false);
                System.Diagnostics.Debug.WriteLine(
                    "[SettingsManager] 已从开机启动移除");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[SettingsManager] 注册表操作失败: {ex.Message}");
        }
    }
}
