using System.IO;
using Microsoft.Data.Sqlite;
using ClipboardManager.Models;

namespace ClipboardManager.Services;

/// <summary>
/// SQLite 数据库服务。
/// 与 Mac 版 DatabaseService 功能完全一致：
/// - 相同 schema、相同索引、相同 WAL 模式
/// - 线程安全（lock 串行化）
/// - 增删改查 + 清理过期记录
/// </summary>
public sealed class DatabaseService
{
    public static DatabaseService Instance { get; } = new();

    private readonly string _dbPath;
    private readonly string _imagesDir;
    private readonly Lock _lock = new(); // .NET 9 新 Lock 类型

    // ── 初始化 ──────────────────────────────────

    private DatabaseService() : this(GetDefaultDataDir()) { }

    /// <summary>测试用构造函数：指定数据目录</summary>
    internal DatabaseService(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        _dbPath = Path.Combine(dataDir, "history.db");
        _imagesDir = Path.Combine(dataDir, "images");
        Directory.CreateDirectory(_imagesDir);

        OpenAndInitialize();
    }

    private static string GetDefaultDataDir() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipboardManager");

    private void OpenAndInitialize()
    {
        using var db = new SqliteConnection($"Data Source={_dbPath}");
        db.Open();

        // WAL 模式提升并发性能
        using var pragmaCmd = db.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA journal_mode=WAL";
        pragmaCmd.ExecuteNonQuery();

        // 建表（与 Mac 版完全一致）
        using var createCmd = db.CreateCommand();
        createCmd.CommandText = """
            CREATE TABLE IF NOT EXISTS clipboard_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                content_type TEXT NOT NULL,
                text_content TEXT,
                image_path TEXT,
                text_hash TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                is_pinned INTEGER DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS idx_created_at
                ON clipboard_items(created_at DESC);
            CREATE INDEX IF NOT EXISTS idx_pinned
                ON clipboard_items(is_pinned);
            """;
        createCmd.ExecuteNonQuery();

        System.Diagnostics.Debug.WriteLine(
            $"[DatabaseService] 数据库已打开: {_dbPath}");
    }

    // ── 公开属性 ─────────────────────────────────

    public string ImagesDir => _imagesDir;

    // ── 保存 ─────────────────────────────────────

    /// <summary>
    /// 保存一条记录。文字去重：比较上一条文字的 hash。
    /// 返回新记录 ID，重复则返回 null。
    /// </summary>
    public int? SaveItem(ContentType contentType,
                         string? textContent = null,
                         string? imagePath = null,
                         string? textHash = null)
    {
        lock (_lock)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            // 去重：检查文字 hash 是否与上一条相同
            if (contentType == ContentType.Text && textHash != null)
            {
                using var checkCmd = db.CreateCommand();
                checkCmd.CommandText = """
                    SELECT text_hash FROM clipboard_items
                    WHERE content_type = 'text'
                    ORDER BY id DESC LIMIT 1
                    """;
                var lastHash = checkCmd.ExecuteScalar() as string;
                if (lastHash == textHash)
                    return null; // 重复，跳过
            }

            // 插入
            using var insertCmd = db.CreateCommand();
            insertCmd.CommandText = """
                INSERT INTO clipboard_items
                    (content_type, text_content, image_path, text_hash)
                VALUES
                    (@type, @text, @image, @hash)
                """;

            insertCmd.Parameters.AddWithValue("@type",
                contentType == ContentType.Text ? "text" : "image");
            insertCmd.Parameters.AddWithValue("@text",
                (object?)textContent ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@image",
                (object?)imagePath ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@hash",
                (object?)textHash ?? DBNull.Value);

            insertCmd.ExecuteNonQuery();

            // 获取新插入 ID
            using var lastIdCmd = db.CreateCommand();
            lastIdCmd.CommandText = "SELECT last_insert_rowid()";
            var newId = Convert.ToInt32(lastIdCmd.ExecuteScalar()!);
            return newId;
        }
    }

    // ── 查询 ─────────────────────────────────────

    /// <summary>
    /// 获取记录列表。置顶在前，时间降序。
    /// 搜索时匹配 text_content LIKE（图片始终保留）。
    /// </summary>
    public List<ClipboardItem> GetItems(string searchQuery = "", int limit = 200)
    {
        lock (_lock)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            using var cmd = db.CreateCommand();
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                cmd.CommandText = """
                    SELECT * FROM clipboard_items
                    ORDER BY is_pinned DESC, created_at DESC
                    LIMIT @limit
                    """;
                cmd.Parameters.AddWithValue("@limit", limit);
            }
            else
            {
                cmd.CommandText = """
                    SELECT * FROM clipboard_items
                    WHERE (text_content LIKE @pattern OR content_type = 'image')
                    ORDER BY is_pinned DESC, created_at DESC
                    LIMIT @limit
                    """;
                cmd.Parameters.AddWithValue("@pattern", $"%{searchQuery}%");
                cmd.Parameters.AddWithValue("@limit", limit);
            }

            var items = new List<ClipboardItem>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(ParseItem(reader));
            }
            return items;
        }
    }

    /// <summary>根据 ID 获取单条记录（复制时使用）</summary>
    public ClipboardItem? GetItem(int id)
    {
        lock (_lock)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT * FROM clipboard_items WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? ParseItem(reader) : null;
        }
    }

    /// <summary>解析行数据为 ClipboardItem</summary>
    private static ClipboardItem ParseItem(SqliteDataReader reader)
    {
        // 列顺序：id(0), content_type(1), text_content(2), image_path(3),
        //          text_hash(4), created_at(5), is_pinned(6)
        return new ClipboardItem
        {
            Id = reader.GetInt32(0),
            Type = reader.GetString(1) == "text"
                ? ContentType.Text : ContentType.Image,
            TextContent = reader.IsDBNull(2) ? null : reader.GetString(2),
            ImagePath = reader.IsDBNull(3) ? null : reader.GetString(3),
            CreatedAt = ParseDateTime(reader.GetString(5)),
            IsPinned = reader.GetInt32(6) != 0
        };
    }

    /// <summary>
    /// 解析 SQLite 日期。支持多种格式：
    /// - ISO8601 with T: 2026-07-20T12:34:56
    /// - ISO8601 with microseconds: 2026-07-20T12:34:56.789012
    /// - Space format: 2026-07-20 12:34:56
    /// </summary>
    private static DateTime ParseDateTime(string dateStr)
    {
        // 统一处理
        var normalized = dateStr.Replace(' ', 'T');

        // 去掉微秒（如果存在）
        var dotIndex = normalized.IndexOf('.');
        if (dotIndex > 0)
        {
            normalized = normalized[..dotIndex];
        }

        if (DateTime.TryParse(normalized, out var result))
            return result;

        return DateTime.Now; // 最终回退
    }

    // ── 置顶切换 ─────────────────────────────────

    /// <summary>切换置顶状态，返回新状态</summary>
    public bool TogglePin(int itemId)
    {
        lock (_lock)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            // 查询当前状态
            using var queryCmd = db.CreateCommand();
            queryCmd.CommandText =
                "SELECT is_pinned FROM clipboard_items WHERE id = @id";
            queryCmd.Parameters.AddWithValue("@id", itemId);
            var current = Convert.ToInt32(queryCmd.ExecuteScalar()!);

            // 翻转
            var newState = current == 0 ? 1 : 0;
            using var updateCmd = db.CreateCommand();
            updateCmd.CommandText =
                "UPDATE clipboard_items SET is_pinned = @state WHERE id = @id";
            updateCmd.Parameters.AddWithValue("@state", newState);
            updateCmd.Parameters.AddWithValue("@id", itemId);
            updateCmd.ExecuteNonQuery();

            return newState != 0;
        }
    }

    // ── 删除 ─────────────────────────────────────

    /// <summary>
    /// 删除一条记录。如果是图片，同时删除磁盘文件。
    /// 与 Mac 版行为一致。
    /// </summary>
    public void DeleteItem(int itemId)
    {
        lock (_lock)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            // 查询图片路径（如有）
            using var queryCmd = db.CreateCommand();
            queryCmd.CommandText =
                "SELECT image_path FROM clipboard_items WHERE id = @id";
            queryCmd.Parameters.AddWithValue("@id", itemId);
            var imagePath = queryCmd.ExecuteScalar() as string;

            // 删除磁盘文件
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try { File.Delete(imagePath); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[DatabaseService] 删除图片失败: {ex.Message}");
                }
            }

            // 删除数据库记录
            using var deleteCmd = db.CreateCommand();
            deleteCmd.CommandText =
                "DELETE FROM clipboard_items WHERE id = @id";
            deleteCmd.Parameters.AddWithValue("@id", itemId);
            deleteCmd.ExecuteNonQuery();
        }
    }

    // ── 统计 ─────────────────────────────────────

    /// <summary>获取总记录数（用于状态栏显示）</summary>
    public int GetTotalCount()
    {
        lock (_lock)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM clipboard_items";
            return Convert.ToInt32(cmd.ExecuteScalar()!);
        }
    }

    // ── 清理 ─────────────────────────────────────

    /// <summary>
    /// 清理过期的非置顶记录。返回清理条数。
    /// 与 Mac 版行为一致：先删图片文件，再删数据库记录。
    /// </summary>
    public int Cleanup(int retentionDays)
    {
        lock (_lock)
        {
            using var db = new SqliteConnection($"Data Source={_dbPath}");
            db.Open();

            var cutoff = DateTime.Now.AddDays(-retentionDays);
            var cutoffStr = cutoff.ToString("yyyy-MM-ddTHH:mm:ss");

            // 收集要删除的图片路径
            var imagePaths = new List<string>();
            using (var queryCmd = db.CreateCommand())
            {
                queryCmd.CommandText = """
                    SELECT image_path FROM clipboard_items
                    WHERE is_pinned = 0 AND created_at < @cutoff
                      AND image_path IS NOT NULL
                    """;
                queryCmd.Parameters.AddWithValue("@cutoff", cutoffStr);

                using var reader = queryCmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                        imagePaths.Add(reader.GetString(0));
                }
            }

            // 删除磁盘文件
            foreach (var path in imagePaths)
            {
                if (File.Exists(path))
                {
                    try { File.Delete(path); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[DatabaseService] 清理图片失败: {ex.Message}");
                    }
                }
            }

            // 删除数据库记录
            using var deleteCmd = db.CreateCommand();
            deleteCmd.CommandText = """
                DELETE FROM clipboard_items
                WHERE is_pinned = 0 AND created_at < @cutoff
                """;
            deleteCmd.Parameters.AddWithValue("@cutoff", cutoffStr);
            var deleted = deleteCmd.ExecuteNonQuery();

            if (deleted > 0)
                System.Diagnostics.Debug.WriteLine(
                    $"[DatabaseService] 清理了 {deleted} 条过期记录");

            return deleted;
        }
    }
}
