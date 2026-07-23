using System.IO;
using ClipboardManager.Models;
using ClipboardManager.Services;
using Xunit;

namespace ClipboardManager.Tests.Services;

public class DatabaseServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DatabaseService _db;

    public DatabaseServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cm_test_{Guid.NewGuid():N}");
        _db = new DatabaseService(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // ── 初始化 ──────────────────────────────────────

    [Fact]
    public void Initialize_CreatesDatabaseFile()
    {
        Assert.True(File.Exists(Path.Combine(_tempDir, "history.db")));
    }

    [Fact]
    public void Initialize_CreatesImagesDirectory()
    {
        Assert.True(Directory.Exists(_db.ImagesDir));
    }

    // ── 保存 ────────────────────────────────────────

    [Fact]
    public void SaveItem_Text_ReturnsNewId()
    {
        var id = _db.SaveItem(ContentType.Text,
            textContent: "Hello World",
            textHash: "abc123");

        Assert.NotNull(id);
        Assert.True(id > 0);
    }

    [Fact]
    public void SaveItem_DuplicateText_ReturnsNull()
    {
        const string hash = "same-hash";

        var id1 = _db.SaveItem(ContentType.Text,
            textContent: "First copy",
            textHash: hash);
        var id2 = _db.SaveItem(ContentType.Text,
            textContent: "Same hash — should be skipped",
            textHash: hash);

        Assert.NotNull(id1);
        Assert.Null(id2); // 重复，跳过
    }

    [Fact]
    public void SaveItem_DifferentText_ReturnsNewId()
    {
        var id1 = _db.SaveItem(ContentType.Text,
            textContent: "Item 1",
            textHash: "hash-1");
        var id2 = _db.SaveItem(ContentType.Text,
            textContent: "Item 2",
            textHash: "hash-2");

        Assert.NotNull(id1);
        Assert.NotNull(id2);
        Assert.NotEqual(id1, id2);
    }

    // ── 查询 ────────────────────────────────────────

    [Fact]
    public void GetItems_ReturnsAllItems()
    {
        _db.SaveItem(ContentType.Text, textContent: "A", textHash: "a");
        _db.SaveItem(ContentType.Text, textContent: "B", textHash: "b");
        _db.SaveItem(ContentType.Text, textContent: "C", textHash: "c");

        var items = _db.GetItems();

        Assert.Equal(3, items.Count);
    }

    [Fact]
    public void GetItems_PinnedFirst()
    {
        var id1 = _db.SaveItem(ContentType.Text, textContent: "Normal", textHash: "n1");
        var id2 = _db.SaveItem(ContentType.Text, textContent: "Pinned", textHash: "p1");

        _db.TogglePin(id2!.Value); // 置顶第二个

        var items = _db.GetItems();

        Assert.True(items[0].IsPinned);
        Assert.Equal(id2.Value, items[0].Id);
    }

    [Fact]
    public void GetItems_WithSearch_FiltersText()
    {
        _db.SaveItem(ContentType.Text, textContent: "Apple Pie", textHash: "h1");
        _db.SaveItem(ContentType.Text, textContent: "Banana Bread", textHash: "h2");
        _db.SaveItem(ContentType.Text, textContent: "Apple Cider", textHash: "h3");

        var items = _db.GetItems("Apple");

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void GetItems_EmptySearch_ReturnsAll()
    {
        _db.SaveItem(ContentType.Text, textContent: "Foo", textHash: "f");
        _db.SaveItem(ContentType.Text, textContent: "Bar", textHash: "b");

        var items = _db.GetItems("");

        Assert.Equal(2, items.Count);
    }

    // ── 置顶 ────────────────────────────────────────

    [Fact]
    public void TogglePin_FlipsState()
    {
        var id = _db.SaveItem(ContentType.Text,
            textContent: "Test", textHash: "h")!.Value;

        var pinned = _db.TogglePin(id);
        Assert.True(pinned);

        var unpinned = _db.TogglePin(id);
        Assert.False(unpinned);
    }

    // ── 删除 ────────────────────────────────────────

    [Fact]
    public void DeleteItem_RemovesRecord()
    {
        var id = _db.SaveItem(ContentType.Text,
            textContent: "To delete", textHash: "d")!.Value;

        _db.DeleteItem(id);

        var items = _db.GetItems();
        Assert.Empty(items);
    }

    [Fact]
    public void DeleteItem_WithImage_CleansFile()
    {
        // 创建假图片文件
        var imgPath = Path.Combine(_db.ImagesDir, "test.png");
        File.WriteAllText(imgPath, "fake png content");

        var id = _db.SaveItem(ContentType.Image, imagePath: imgPath)!.Value;

        _db.DeleteItem(id);

        Assert.False(File.Exists(imgPath));
    }

    // ── 统计 ────────────────────────────────────────

    [Fact]
    public void GetTotalCount_ReturnsCorrectCount()
    {
        Assert.Equal(0, _db.GetTotalCount());

        _db.SaveItem(ContentType.Text, textContent: "A", textHash: "a");
        _db.SaveItem(ContentType.Text, textContent: "B", textHash: "b");

        Assert.Equal(2, _db.GetTotalCount());
    }

    // ── 清理过期 ─────────────────────────────────────

    [Fact]
    public void Cleanup_RemovesExpiredNonPinnedItems()
    {
        // 插入一条新记录
        _db.SaveItem(ContentType.Text, textContent: "Recent", textHash: "r");
        var totalBefore = _db.GetTotalCount();

        // 清理：保留 0 天 → 删除所有非置顶记录
        var deleted = _db.Cleanup(0);

        Assert.Equal(totalBefore, deleted);
        Assert.Equal(0, _db.GetTotalCount());
    }

    [Fact]
    public void Cleanup_PreservesPinnedItems()
    {
        var id = _db.SaveItem(ContentType.Text,
            textContent: "Pinned forever", textHash: "pf")!.Value;
        _db.TogglePin(id);

        _db.Cleanup(0); // 删除所有非置顶

        Assert.Equal(1, _db.GetTotalCount());
    }

    // ── GetItem ─────────────────────────────────────

    [Fact]
    public void GetItem_ById_ReturnsCorrectItem()
    {
        var id = _db.SaveItem(ContentType.Text,
            textContent: "Find me", textHash: "find")!.Value;

        var item = _db.GetItem(id);

        Assert.NotNull(item);
        Assert.Equal("Find me", item.TextContent);
        Assert.Equal(ContentType.Text, item.Type);
    }

    [Fact]
    public void GetItem_NotFound_ReturnsNull()
    {
        var item = _db.GetItem(99999);
        Assert.Null(item);
    }
}
