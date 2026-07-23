using ClipboardManager.Models;
using Xunit;

namespace ClipboardManager.Tests.Models;

public class ClipboardItemTests
{
    // ── Preview 属性 ─────────────────────────────────

    [Fact]
    public void Preview_ShortText_ReturnsFullText()
    {
        var item = new ClipboardItem
        {
            Type = ContentType.Text,
            TextContent = "Hello"
        };

        Assert.Equal("Hello", item.Preview);
    }

    [Fact]
    public void Preview_LongText_TruncatesAt100()
    {
        var longText = new string('A', 200);
        var item = new ClipboardItem
        {
            Type = ContentType.Text,
            TextContent = longText
        };

        Assert.Equal(103, item.Preview.Length);
        Assert.True(item.Preview.EndsWith("..."));
    }

    [Fact]
    public void Preview_Exactly100Chars_ReturnsFull()
    {
        var text = new string('X', 100);
        var item = new ClipboardItem
        {
            Type = ContentType.Text,
            TextContent = text
        };

        Assert.Equal(text, item.Preview);
    }

    [Fact]
    public void Preview_NullText_ReturnsEmpty()
    {
        var item = new ClipboardItem
        {
            Type = ContentType.Text,
            TextContent = null
        };

        Assert.Equal("[空]", item.Preview);
    }

    [Fact]
    public void Preview_EmptyText_ReturnsPlaceholder()
    {
        var item = new ClipboardItem
        {
            Type = ContentType.Text,
            TextContent = ""
        };

        Assert.Equal("[空]", item.Preview);
    }

    [Fact]
    public void Preview_Image_ReturnsPlaceholder()
    {
        var item = new ClipboardItem
        {
            Type = ContentType.Image,
            ImagePath = "/some/path.png"
        };

        Assert.Equal("[图片]", item.Preview);
    }

    // ── RelativeTime 属性 ───────────────────────────

    [Fact]
    public void RelativeTime_JustNow()
    {
        var item = new ClipboardItem
        {
            CreatedAt = DateTime.Now.AddSeconds(-5)
        };

        Assert.Equal("刚刚", item.RelativeTime);
    }

    [Fact]
    public void RelativeTime_MinutesAgo()
    {
        var item = new ClipboardItem
        {
            CreatedAt = DateTime.Now.AddMinutes(-3)
        };

        Assert.Equal("3分钟前", item.RelativeTime);
    }

    [Fact]
    public void RelativeTime_HoursAgo()
    {
        var item = new ClipboardItem
        {
            CreatedAt = DateTime.Now.AddHours(-2).AddMinutes(-30)
        };

        Assert.Equal("2小时前", item.RelativeTime);
    }

    [Fact]
    public void RelativeTime_DaysAgo()
    {
        var item = new ClipboardItem
        {
            CreatedAt = DateTime.Now.AddDays(-2)
        };

        Assert.Equal("2天前", item.RelativeTime);
    }
}
