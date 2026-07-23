using System.IO;
using ClipboardManager.Services;
using Xunit;

namespace ClipboardManager.Tests.Services;

public class SettingsManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsManager _settings;

    public SettingsManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cm_settings_{Guid.NewGuid():N}");
        _settings = new SettingsManager(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // ── 默认值 ──────────────────────────────────────

    [Fact]
    public void Default_RetentionDays_Is3()
    {
        Assert.Equal(3, _settings.RetentionDays);
    }

    [Fact]
    public void Default_LaunchAtLogin_IsTrue()
    {
        Assert.True(_settings.LaunchAtLogin);
    }

    // ── 读写 ────────────────────────────────────────

    [Fact]
    public void RetentionDays_SetAndGet()
    {
        _settings.RetentionDays = 5;
        Assert.Equal(5, _settings.RetentionDays);

        _settings.RetentionDays = 1;
        Assert.Equal(1, _settings.RetentionDays);
    }

    [Fact]
    public void LaunchAtLogin_SetAndGet()
    {
        _settings.LaunchAtLogin = false;
        Assert.False(_settings.LaunchAtLogin);

        _settings.LaunchAtLogin = true;
        Assert.True(_settings.LaunchAtLogin);
    }

    // ── 持久化 ──────────────────────────────────────

    [Fact]
    public void Settings_SurviveReload()
    {
        _settings.RetentionDays = 1;
        _settings.LaunchAtLogin = false;

        // 重新加载
        var reloaded = new SettingsManager(_tempDir);

        Assert.Equal(1, reloaded.RetentionDays);
        Assert.False(reloaded.LaunchAtLogin);
    }

    [Fact]
    public void SettingsFile_IsCreated()
    {
        _settings.RetentionDays = 5; // 触发保存

        var settingsPath = Path.Combine(_tempDir, "settings.json");
        Assert.True(File.Exists(settingsPath));
    }

    // ── ApplyLoginItem ──────────────────────────────

    [Fact]
    public void ApplyLoginItem_DoesNotThrow()
    {
        // 注册表可能不可用（非 Windows / 无权限），不应抛异常
        var ex = Record.Exception(() => _settings.ApplyLoginItem());
        Assert.Null(ex);
    }
}
