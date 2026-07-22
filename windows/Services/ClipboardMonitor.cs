using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ClipboardManager.Models;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using TextDataFormat = System.Windows.TextDataFormat;

namespace ClipboardManager.Services;

/// <summary>
/// 剪贴板监控服务：500ms 轮询，检测文字/图片变动，SHA256 去重。
/// 与 Mac 版 ClipboardMonitor 功能完全一致。
///
/// 使用 Win32 GetClipboardSequenceNumber() 检测变化（等同 NSPasteboard.changeCount）。
/// DispatcherTimer 确保在 UI 线程访问剪贴板（WPF STA 要求）。
/// </summary>
public sealed class ClipboardMonitor
{
    public static ClipboardMonitor Instance { get; } = new();

    private DispatcherTimer? _timer;
    private uint _lastSequenceNumber;
    private string _lastTextHash = string.Empty;
    private string _lastImageHash = string.Empty;
    private bool _ignoreNextChange;
    private int _saveCount;

    /// <summary>当新记录添加时触发（UI 刷新用）</summary>
    public event Action? NewItemAdded;

    // ── Win32 P/Invoke ────────────────────────────

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".heic", ".heif"
    };

    private ClipboardMonitor() { }

    // ── 启动/停止 ─────────────────────────────────

    public void Start()
    {
        _lastSequenceNumber = GetClipboardSequenceNumber();
        UpdateLastState();

        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(500),
            DispatcherPriority.Background,
            OnTick,
            Application.Current.Dispatcher);

        Debug.WriteLine("[ClipboardMonitor] 监控已启动 (500ms 轮询)");
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
        Debug.WriteLine("[ClipboardMonitor] 监控已停止");
    }

    /// <summary>标记下一次变动忽略（从本应用复制回剪贴板时调用）</summary>
    public void IgnoreOnce()
    {
        _ignoreNextChange = true;
    }

    // ── 轮询回调 ──────────────────────────────────

    private void OnTick(object? sender, EventArgs e)
    {
        var currentSeq = GetClipboardSequenceNumber();
        if (currentSeq == _lastSequenceNumber)
            return;
        _lastSequenceNumber = currentSeq;

        // 忽略自触发变动
        if (_ignoreNextChange)
        {
            _ignoreNextChange = false;
            UpdateLastState();
            return;
        }

        try
        {
            // 1. 检测图片（优先，因为图片也可能包含文字格式）
            if (TryReadImage(out var imageData, out var hash))
            {
                if (hash != _lastImageHash && !string.IsNullOrEmpty(hash))
                {
                    _lastImageHash = hash;
                    _lastTextHash = string.Empty;
                    SaveImage(imageData, hash);
                    return;
                }
            }

            // 2. 检测文件拖放（从资源管理器复制的图片文件）
            if (TryReadImageFiles(out var filePaths))
            {
                // 只处理第一个图片文件
                foreach (var filePath in filePaths)
                {
                    var fileHash = HashFile(filePath);
                    if (fileHash != _lastImageHash && !string.IsNullOrEmpty(fileHash))
                    {
                        _lastImageHash = fileHash;
                        _lastTextHash = string.Empty;
                        SaveImageFromFile(filePath);
                    }
                    break;
                }
                return;
            }

            // 3. 检测文字
            if (Clipboard.ContainsText(TextDataFormat.UnicodeText) ||
                Clipboard.ContainsText(TextDataFormat.Text))
            {
                var text = Clipboard.GetText(TextDataFormat.UnicodeText)
                        ?? Clipboard.GetText(TextDataFormat.Text);

                if (!string.IsNullOrEmpty(text))
                {
                    var textHash = HashText(text);
                    if (textHash != _lastTextHash && !string.IsNullOrEmpty(textHash))
                    {
                        _lastTextHash = textHash;
                        _lastImageHash = string.Empty;
                        SaveText(text, textHash);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitor] 读取剪贴板异常: {ex.Message}");
        }
    }

    private void UpdateLastState()
    {
        try
        {
            if (TryReadImage(out _, out var hash))
            {
                _lastImageHash = hash;
                _lastTextHash = string.Empty;
            }
            else if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                var text = Clipboard.GetText(TextDataFormat.UnicodeText) ?? string.Empty;
                _lastTextHash = HashText(text);
                _lastImageHash = string.Empty;
            }
        }
        catch
        {
            // 忽略初始化时的剪贴板读取错误
        }
    }

    // ── 读取：图片 ─────────────────────────────────

    private static bool TryReadImage(out byte[] pngData, out string hash)
    {
        pngData = Array.Empty<byte>();
        hash = string.Empty;

        try
        {
            if (!Clipboard.ContainsImage())
                return false;

            // WPF Clipboard.GetImage() → BitmapSource
            var bitmapSource = Clipboard.GetImage();
            if (bitmapSource == null)
                return false;

            // 编码为 PNG 字节
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            pngData = ms.ToArray();

            hash = HashImagePng(pngData);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadImageFiles(out List<string> filePaths)
    {
        filePaths = new List<string>();

        try
        {
            if (!Clipboard.ContainsFileDropList())
                return false;

            var files = Clipboard.GetFileDropList();
            foreach (var file in files)
            {
                if (File.Exists(file) && IsImageFile(file))
                    filePaths.Add(file);
            }
            return filePaths.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsImageFile(string path)
    {
        var ext = Path.GetExtension(path);
        return ImageExtensions.Contains(ext);
    }

    // ── 保存 ──────────────────────────────────────

    private void SaveText(string text, string hash)
    {
        var itemId = DatabaseService.Instance.SaveItem(
            ContentType.Text,
            textContent: text,
            textHash: hash);

        if (itemId.HasValue)
        {
            MaybeCleanup();
            OnNewItem();
            Debug.WriteLine(
                $"[ClipboardMonitor] 文字已保存: {Truncate(text, 50)}");
        }
    }

    private void SaveImage(byte[] pngData, string hash)
    {
        var filepath = WriteImageToDisk(pngData);
        if (filepath == null) return;

        var itemId = DatabaseService.Instance.SaveItem(
            ContentType.Image,
            imagePath: filepath);

        if (itemId.HasValue)
        {
            MaybeCleanup();
            OnNewItem();
            Debug.WriteLine($"[ClipboardMonitor] 图片已保存: {filepath}");
        }
        else
        {
            // 保存失败，清理文件
            try { File.Delete(filepath); }
            catch { /* ignore */ }
        }
    }

    private void SaveImageFromFile(string sourcePath)
    {
        // 直接复制文件，保留原始格式和质量（与 Mac 版一致）
        var imagesDir = DatabaseService.Instance.ImagesDir;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_ffffff");
        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (ext == ".jpeg") ext = ".jpg";

        var destPath = Path.Combine(imagesDir, $"{timestamp}{ext}");

        try
        {
            File.Copy(sourcePath, destPath, overwrite: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitor] 图片复制失败: {ex.Message}");
            return;
        }

        var itemId = DatabaseService.Instance.SaveItem(
            ContentType.Image,
            imagePath: destPath);

        if (itemId.HasValue)
        {
            MaybeCleanup();
            OnNewItem();
            Debug.WriteLine($"[ClipboardMonitor] 图片已保存(文件): {destPath}");
        }
        else
        {
            try { File.Delete(destPath); }
            catch { /* ignore */ }
        }
    }

    private static string? WriteImageToDisk(byte[] pngData)
    {
        var imagesDir = DatabaseService.Instance.ImagesDir;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_ffffff");
        var filepath = Path.Combine(imagesDir, $"{timestamp}.png");

        try
        {
            File.WriteAllBytes(filepath, pngData);

            // 验证文件
            var info = new FileInfo(filepath);
            if (info.Length == 0)
            {
                File.Delete(filepath);
                return null;
            }
            return filepath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitor] 图片写入失败: {ex.Message}");
            return null;
        }
    }

    // ── 哈希（去重）────────────────────────────────

    private static string HashText(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    /// <summary>
    /// 图片哈希：将 PNG 缩放到 32×32 后计算 SHA256。
    /// 与 Mac 版逻辑一致（缩略图哈希，减少计算量 + 容忍轻微压缩差异）。
    /// </summary>
    private static string HashImagePng(byte[] pngData)
    {
        try
        {
            // 解码 PNG
            using var ms = new MemoryStream(pngData);
            var decoder = new PngBitmapDecoder(
                ms,
                BitmapCreateOptions.None,
                BitmapCacheOption.OnLoad);
            var source = decoder.Frames[0];

            // 缩放到 32×32
            var thumbnail = new TransformedBitmap(
                source,
                new System.Windows.Media.ScaleTransform(
                    32.0 / source.PixelWidth,
                    32.0 / source.PixelHeight));

            // 重新编码
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(thumbnail));

            using var outMs = new MemoryStream();
            encoder.Save(outMs);
            var thumbData = outMs.ToArray();

            return Convert.ToHexString(SHA256.HashData(thumbData));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitor] 图片哈希失败: {ex.Message}");
            return string.Empty;
        }
    }

    private static string HashFile(string filePath)
    {
        try
        {
            var bytes = File.ReadAllBytes(filePath);

            // 如果文件较大，只读前 64KB（足够去重）
            if (bytes.Length > 65536)
                Array.Resize(ref bytes, 65536);

            return Convert.ToHexString(SHA256.HashData(bytes));
        }
        catch
        {
            return string.Empty;
        }
    }

    // ── 清理与通知 ─────────────────────────────────

    private void MaybeCleanup()
    {
        _saveCount++;
        if (_saveCount >= 50)
        {
            _saveCount = 0;
            var days = SettingsManager.Instance.RetentionDays;
            Task.Run(() => DatabaseService.Instance.Cleanup(days));
        }
    }

    private void OnNewItem()
    {
        NewItemAdded?.Invoke();
    }

    // ── Helpers ───────────────────────────────────

    private static string Truncate(string text, int maxLen)
    {
        if (text.Length <= maxLen) return text;
        return string.Concat(text.AsSpan(0, maxLen), "...");
    }
}
