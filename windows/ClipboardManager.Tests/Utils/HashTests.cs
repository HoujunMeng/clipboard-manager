using System.Security.Cryptography;
using System.Text;
using ClipboardManager.Services;
using Xunit;

namespace ClipboardManager.Tests.Utils;

public class HashTests
{
    // 最小合法 1×1 白色 PNG（用于有效 PNG 测试）
    private static readonly byte[] ValidPng1x1 = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==");

    // 最小合法 1×1 黑色 PNG（用于不同图片测试）
    private static readonly byte[] ValidPng1x1Black = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    // ── 文字哈希 ─────────────────────────────────────

    [Fact]
    public void HashText_SameInput_ReturnsSameHash()
    {
        var hash1 = ClipboardMonitor.HashText("Hello World");
        var hash2 = ClipboardMonitor.HashText("Hello World");

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);
    }

    [Fact]
    public void HashText_DifferentInput_ReturnsDifferentHash()
    {
        var hash1 = ClipboardMonitor.HashText("Hello World");
        var hash2 = ClipboardMonitor.HashText("hello world");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashText_EmptyString_MatchesRawSHA256()
    {
        var hash = ClipboardMonitor.HashText("");
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("")));
        Assert.Equal(expected, hash);
    }

    [Fact]
    public void HashText_ChineseCharacters_ReturnsValidHash()
    {
        var hash = ClipboardMonitor.HashText("你好世界");
        Assert.Equal(64, hash.Length);
    }

    // ── 图片哈希（不依赖 WPF，使用硬编码 PNG 字节）────

    [Fact]
    public void HashImagePng_ValidPng_ReturnsNonEmptyHash()
    {
        var hash = ClipboardMonitor.HashImagePng(ValidPng1x1);
        Assert.True(hash.Length > 0);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void HashImagePng_SameImage_ReturnsSameHash()
    {
        var hash1 = ClipboardMonitor.HashImagePng(ValidPng1x1);
        var hash2 = ClipboardMonitor.HashImagePng(ValidPng1x1);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashImagePng_DifferentImages_ReturnDifferentHashes()
    {
        var hash1 = ClipboardMonitor.HashImagePng(ValidPng1x1);
        var hash2 = ClipboardMonitor.HashImagePng(ValidPng1x1Black);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashImagePng_InvalidData_ReturnsEmpty()
    {
        byte[] badData = [0x00, 0x01, 0x02];
        var hash = ClipboardMonitor.HashImagePng(badData);
        Assert.Equal(string.Empty, hash);
    }

    [Fact]
    public void HashImagePng_EmptyArray_ReturnsEmpty()
    {
        var hash = ClipboardMonitor.HashImagePng([]);
        Assert.Equal(string.Empty, hash);
    }

    // ── 文件哈希 ─────────────────────────────────────

    [Fact]
    public void HashFile_SameFile_ReturnsSameHash()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "test content");
            var hash1 = ClipboardMonitor.HashFile(path);
            var hash2 = ClipboardMonitor.HashFile(path);
            Assert.Equal(hash1, hash2);
            Assert.Equal(64, hash1.Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void HashFile_NotExists_ReturnsEmpty()
    {
        var hash = ClipboardMonitor.HashFile(
            Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.txt"));
        Assert.Equal(string.Empty, hash);
    }

    [Fact]
    public void HashFile_LargeFile_OnlyHashesFirst64K()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = new byte[100_000];
            Random.Shared.NextBytes(data);
            File.WriteAllBytes(path, data);

            var hash = ClipboardMonitor.HashFile(path);
            Assert.Equal(64, hash.Length);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
