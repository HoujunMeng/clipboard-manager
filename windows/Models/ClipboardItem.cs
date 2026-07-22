namespace ClipboardManager.Models;

/// <summary>
/// 剪贴板记录数据模型。
/// 与 Mac 版 ClipboardItem (Swift struct) 字段和能力完全一致。
/// </summary>
public sealed class ClipboardItem
{
    public int Id { get; init; }
    public ContentType Type { get; init; }
    public string? TextContent { get; init; }
    public string? ImagePath { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsPinned { get; init; }

    /// <summary>文字预览（最多 100 字，换行统一为空格）</summary>
    public string Preview
    {
        get
        {
            return Type switch
            {
                ContentType.Text => TruncatePreview(TextContent),
                ContentType.Image => "[图片]",
                _ => "[未知]"
            };
        }
    }

    /// <summary>相对时间描述（中文，与 Python/Swift 版逻辑一致）</summary>
    public string RelativeTime
    {
        get
        {
            var diff = DateTime.Now - CreatedAt;

            if (diff.TotalSeconds < 60)
                return "刚刚";

            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}分钟前";

            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}小时前";

            if (diff.TotalDays < 2)
                return $"昨天 {CreatedAt:HH:mm}";

            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}天前";

            return CreatedAt.ToString("MM月dd日 HH:mm");
        }
    }

    // ── Helpers ──────────────────────────────────

    private static string TruncatePreview(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "[空]";

        // 将换行统一为空格（与 Mac 版一致）
        var singleLine = text
            .Replace("\r\n", " ")
            .Replace('\n', ' ')
            .Replace('\r', ' ');

        if (singleLine.Length > 100)
            return string.Concat(singleLine.AsSpan(0, 100), "...");

        return singleLine;
    }

    public override string ToString()
        => $"[{Id}] {Preview} ({RelativeTime})";
}

/// <summary>内容类型枚举</summary>
public enum ContentType
{
    Text = 0,
    Image = 1
}
