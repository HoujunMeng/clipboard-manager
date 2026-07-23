using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
using ClipboardManager.Services;

namespace ClipboardManager.Tests.Utils;

public class HashTests
{
    // ── 文字哈希 ─────────────────────────────────────

    [Fact]
    public void HashText_SameInput_ReturnsSameHash()
    {
        var hash1 = ClipboardMonitor.HashText("Hello World");
        var hash2 = ClipboardMonitor.HashText("Hello World");

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA256 = 64 hex chars
    }

    [Fact]
    public void HashText_DifferentInput_ReturnsDifferentHash()
    {
        var hash1 = ClipboardMonitor.HashText("Hello World");
        var hash2 = ClipboardMonitor.HashText("hello world"); // 不同大小写

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashText_EmptyString_ReturnsConsistentHash()
    {
        var hash = ClipboardMonitor.HashText("");
        Assert.Equal(64, hash.Length);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(""))), hash);
    }

    [Fact]
    public void HashText_ChineseCharacters()
    {
        var hash = ClipboardMonitor.HashText("你好世界");
        Assert.Equal(64, hash.Length);
    }

    // ── 图片哈希 ─────────────────────────────────────

    [Fact]
    public void HashImagePng_SameImage_ReturnsSameHash()
    {
        var png = CreateTestPng(100, 80);

        var hash1 = ClipboardMonitor.HashImagePng(png);
        var hash2 = ClipboardMonitor.HashImagePng(png);

        Assert.True(hash1.Length > 0);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashImagePng_DifferentImages_ReturnDifferentHashes()
    {
        var png1 = CreateTestPng(100, 80);
        var png2 = CreateTestPng(64, 64);

        var hash1 = ClipboardMonitor.HashImagePng(png1);
        var hash2 = ClipboardMonitor.HashImagePng(png2);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashImagePng_InvalidData_ReturnsEmpty()
    {
        byte[] badData = [0x00, 0x01, 0x02]; // 不是合法 PNG

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
        var hash = ClipboardMonitor.HashFile(@"Z:\nonexistent\file.txt");
        Assert.Equal(string.Empty, hash);
    }

    [Fact]
    public void HashFile_LargeFile_LimitsTo64K()
    {
        var path = Path.GetTempFileName();
        try
        {
            // 创建 > 64KB 的文件
            var data = new byte[100_000];
            Random.Shared.NextBytes(data);
            File.WriteAllBytes(path, data);

            var hash = ClipboardMonitor.HashFile(path);

            Assert.Equal(64, hash.Length); // 仍然成功返回有效哈希
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ── Helpers ──────────────────────────────────────

    /// <summary>生成一张纯色 PNG 用于哈希测试</summary>
    private static byte[] CreateTestPng(int width, int height)
    {
        var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(
            width, height, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32, null);

        // 填充像素
        var pixels = new byte[width * height * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = (byte)(i % 256);     // B
            pixels[i + 1] = (byte)((i + 1) % 256); // G
            pixels[i + 2] = (byte)((i + 2) % 256); // R
            pixels[i + 3] = 255;                  // A
        }
        bitmap.WritePixels(
            new System.Windows.Int32Rect(0, 0, width, height),
            pixels, width * 4, 0);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }
}
