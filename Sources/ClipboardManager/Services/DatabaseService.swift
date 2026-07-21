import Foundation
import SQLite3

/// SQLite 数据库服务：增删改查 + 自动清理
/// 与 Python 版使用相同的数据库 schema，数据可互通
final class DatabaseService {
    static let shared = DatabaseService()

    private var db: OpaquePointer?
    private let queue = DispatchQueue(label: "com.clipboardmanager.db", qos: .utility)

    // MARK: — 路径

    private var dataDir: String {
        let path = NSHomeDirectory() + "/Library/Application Support/ClipboardManager"
        try? FileManager.default.createDirectory(
            atPath: path, withIntermediateDirectories: true)
        return path
    }

    private var dbPath: String {
        dataDir + "/history.db"
    }

    var imagesDir: String {
        let path = dataDir + "/images"
        try? FileManager.default.createDirectory(
            atPath: path, withIntermediateDirectories: true)
        return path
    }

    // MARK: — 初始化

    private init() {
        openDatabase()
        createTables()
    }

    deinit {
        if let db = db {
            sqlite3_close(db)
        }
    }

    private func openDatabase() {
        guard sqlite3_open(dbPath, &db) == SQLITE_OK else {
            print("[DatabaseService] 无法打开数据库: \(dbPath)")
            return
        }
        // WAL 模式提升并发性能
        sqlite3_exec(db, "PRAGMA journal_mode=WAL", nil, nil, nil)
        print("[DatabaseService] 数据库已打开: \(dbPath)")
    }

    private func createTables() {
        let sql = """
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
        """
        execute(sql: sql)
    }

    // MARK: — 执行 SQL

    @discardableResult
    private func execute(sql: String) -> Bool {
        guard let db = db else { return false }
        return sqlite3_exec(db, sql, nil, nil, nil) == SQLITE_OK
    }

    // MARK: — 保存记录

    /// 保存一条剪贴板记录。返回新记录的 ID，如果重复则返回 nil。
    func saveItem(contentType: ClipboardItem.ContentType,
                  textContent: String? = nil,
                  imagePath: String? = nil,
                  textHash: String? = nil) -> Int? {
        return queue.sync {
            guard let db = db else { return nil }

            // 去重：检查是否与上一条文字记录相同
            if let hash = textHash {
                let checkSQL = """
                    SELECT text_hash FROM clipboard_items
                    WHERE content_type = 'text' ORDER BY id DESC LIMIT 1
                """
                var stmt: OpaquePointer?
                if sqlite3_prepare_v2(db, checkSQL, -1, &stmt, nil) == SQLITE_OK {
                    if sqlite3_step(stmt) == SQLITE_ROW,
                       let lastHash = sqlite3_column_text(stmt, 0) {
                        if String(cString: lastHash) == hash {
                            sqlite3_finalize(stmt)
                            return nil // 重复内容
                        }
                    }
                    sqlite3_finalize(stmt)
                }
            }

            let insertSQL = """
                INSERT INTO clipboard_items (content_type, text_content, image_path, text_hash)
                VALUES (?, ?, ?, ?)
            """
            var stmt: OpaquePointer?
            guard sqlite3_prepare_v2(db, insertSQL, -1, &stmt, nil) == SQLITE_OK else {
                print("[DatabaseService] prepare INSERT 失败")
                return nil
            }
            defer { sqlite3_finalize(stmt) }

            let typeStr = contentType.rawValue
            sqlite3_bind_text(stmt, 1, (typeStr as NSString).utf8String, -1, nil)

            if let text = textContent {
                sqlite3_bind_text(stmt, 2, (text as NSString).utf8String, -1, nil)
            } else {
                sqlite3_bind_null(stmt, 2)
            }

            if let path = imagePath {
                sqlite3_bind_text(stmt, 3, (path as NSString).utf8String, -1, nil)
            } else {
                sqlite3_bind_null(stmt, 3)
            }

            if let hash = textHash {
                sqlite3_bind_text(stmt, 4, (hash as NSString).utf8String, -1, nil)
            } else {
                sqlite3_bind_null(stmt, 4)
            }

            guard sqlite3_step(stmt) == SQLITE_DONE else {
                print("[DatabaseService] INSERT 失败")
                return nil
            }

            let newId = Int(sqlite3_last_insert_rowid(db))
            return newId
        }
    }

    // MARK: — 查询记录

    /// 获取剪贴板记录列表。置顶在前，按时间降序排列。
    func getItems(searchQuery: String = "", limit: Int = 200) -> [ClipboardItem] {
        return queue.sync {
            guard let db = db else { return [] }

            let sql: String
            if searchQuery.isEmpty {
                sql = """
                    SELECT * FROM clipboard_items
                    ORDER BY is_pinned DESC, created_at DESC
                    LIMIT ?
                """
            } else {
                sql = """
                    SELECT * FROM clipboard_items
                    WHERE (text_content LIKE ? OR content_type = 'image')
                    ORDER BY is_pinned DESC, created_at DESC
                    LIMIT ?
                """
            }

            var stmt: OpaquePointer?
            guard sqlite3_prepare_v2(db, sql, -1, &stmt, nil) == SQLITE_OK else {
                return []
            }
            defer { sqlite3_finalize(stmt) }

            if !searchQuery.isEmpty {
                let pattern = "%\(searchQuery)%"
                sqlite3_bind_text(stmt, 1, (pattern as NSString).utf8String, -1, nil)
                sqlite3_bind_int(stmt, 2, Int32(limit))
            } else {
                sqlite3_bind_int(stmt, 1, Int32(limit))
            }

            var items: [ClipboardItem] = []
            let dateFormatter = ISO8601DateFormatter()

            while sqlite3_step(stmt) == SQLITE_ROW {
                guard let item = parseItem(stmt: stmt!, dateFormatter: dateFormatter) else {
                    continue
                }
                items.append(item)
            }
            return items
        }
    }

    /// 根据 ID 获取单条记录
    func getItem(id: Int) -> ClipboardItem? {
        return queue.sync {
            guard let db = db else { return nil }

            let sql = "SELECT * FROM clipboard_items WHERE id = ?"
            var stmt: OpaquePointer?
            guard sqlite3_prepare_v2(db, sql, -1, &stmt, nil) == SQLITE_OK else {
                return nil
            }
            defer { sqlite3_finalize(stmt) }

            sqlite3_bind_int(stmt, 1, Int32(id))

            guard sqlite3_step(stmt) == SQLITE_ROW else { return nil }

            let dateFormatter = ISO8601DateFormatter()
            return parseItem(stmt: stmt!, dateFormatter: dateFormatter)
        }
    }

    /// 解析一行数据库记录为 ClipboardItem
    private func parseItem(stmt: OpaquePointer,
                           dateFormatter: ISO8601DateFormatter) -> ClipboardItem? {
        let id = Int(sqlite3_column_int(stmt, 0))

        guard let typeCStr = sqlite3_column_text(stmt, 1) else { return nil }
        let typeStr = String(cString: typeCStr)
        let contentType = ClipboardItem.ContentType(rawValue: typeStr) ?? .text

        var textContent: String? = nil
        if let textCStr = sqlite3_column_text(stmt, 2) {
            textContent = String(cString: textCStr)
        }

        var imagePath: String? = nil
        if let pathCStr = sqlite3_column_text(stmt, 3) {
            imagePath = String(cString: pathCStr)
        }

        // text_hash 列 (索引 4)，略过

        var createdAt = Date()
        if let dateCStr = sqlite3_column_text(stmt, 5) {
            // SQLite 存储格式：2026-07-20T12:34:56.789012
            var dateStr = String(cString: dateCStr)
            // 确保 ISO8601DateFormatter 可以解析
            if !dateStr.contains("T") {
                dateStr = dateStr.replacingOccurrences(of: " ", with: "T")
            }

            // ISO8601DateFormatter 需要精确格式
            if let date = dateFormatter.date(from: dateStr) {
                createdAt = date
            } else {
                // 尝试处理带微秒的格式
                let withZ = dateStr + "Z"
                if let date = dateFormatter.date(from: withZ) {
                    createdAt = date
                } else {
                    // 最后回退：手动解析
                    let trimmed = dateStr.components(separatedBy: ".").first ?? dateStr
                    if let date = dateFormatter.date(from: trimmed) {
                        createdAt = date
                    }
                }
            }
        }

        let isPinned = sqlite3_column_int(stmt, 6) != 0

        return ClipboardItem(
            id: id,
            contentType: contentType,
            textContent: textContent,
            imagePath: imagePath,
            createdAt: createdAt,
            isPinned: isPinned
        )
    }

    // MARK: — 置顶切换

    /// 切换置顶状态，返回新状态
    func togglePin(itemId: Int) -> Bool {
        return queue.sync {
            guard let db = db else { return false }

            // 查询当前状态
            let querySQL = "SELECT is_pinned FROM clipboard_items WHERE id = ?"
            var stmt: OpaquePointer?
            guard sqlite3_prepare_v2(db, querySQL, -1, &stmt, nil) == SQLITE_OK else {
                return false
            }
            defer { sqlite3_finalize(stmt) }

            sqlite3_bind_int(stmt, 1, Int32(itemId))
            guard sqlite3_step(stmt) == SQLITE_ROW else { return false }

            let current = sqlite3_column_int(stmt, 0)
            let newState: Int32 = current == 0 ? 1 : 0

            // 更新状态
            let updateSQL = "UPDATE clipboard_items SET is_pinned = ? WHERE id = ?"
            var updateStmt: OpaquePointer?
            guard sqlite3_prepare_v2(db, updateSQL, -1, &updateStmt, nil) == SQLITE_OK else {
                return false
            }
            defer { sqlite3_finalize(updateStmt) }

            sqlite3_bind_int(updateStmt, 1, newState)
            sqlite3_bind_int(updateStmt, 2, Int32(itemId))
            sqlite3_step(updateStmt)

            return newState != 0
        }
    }

    // MARK: — 删除记录

    /// 删除一条记录，同时清理磁盘图片文件
    func deleteItem(itemId: Int) {
        queue.sync {
            guard let db = db else { return }

            // 先查图片路径
            let querySQL = "SELECT image_path FROM clipboard_items WHERE id = ?"
            var stmt: OpaquePointer?
            if sqlite3_prepare_v2(db, querySQL, -1, &stmt, nil) == SQLITE_OK {
                sqlite3_bind_int(stmt, 1, Int32(itemId))
                if sqlite3_step(stmt) == SQLITE_ROW,
                   let pathCStr = sqlite3_column_text(stmt, 0) {
                    let imagePath = String(cString: pathCStr)
                    // 删除磁盘文件
                    if FileManager.default.fileExists(atPath: imagePath) {
                        try? FileManager.default.removeItem(atPath: imagePath)
                    }
                }
                sqlite3_finalize(stmt)
            }

            // 删除数据库记录
            let deleteSQL = "DELETE FROM clipboard_items WHERE id = ?"
            if sqlite3_prepare_v2(db, deleteSQL, -1, &stmt, nil) == SQLITE_OK {
                sqlite3_bind_int(stmt, 1, Int32(itemId))
                sqlite3_step(stmt)
                sqlite3_finalize(stmt)
            }
        }
    }

    // MARK: — 统计与清理

    /// 获取总记录数
    func getTotalCount() -> Int {
        return queue.sync {
            guard let db = db else { return 0 }
            var stmt: OpaquePointer?
            let sql = "SELECT COUNT(*) FROM clipboard_items"
            guard sqlite3_prepare_v2(db, sql, -1, &stmt, nil) == SQLITE_OK else {
                return 0
            }
            defer { sqlite3_finalize(stmt) }

            guard sqlite3_step(stmt) == SQLITE_ROW else { return 0 }
            return Int(sqlite3_column_int(stmt, 0))
        }
    }

    /// 清理过期的非置顶记录。返回清理条数。
    @discardableResult
    func cleanup(retentionDays: Int) -> Int {
        return queue.sync {
            guard let db = db else { return 0 }

            let cutoff = Date().addingTimeInterval(
                -Double(retentionDays) * 86400)
            let dateFormatter = ISO8601DateFormatter()
            let cutoffStr = dateFormatter.string(from: cutoff)

            // 先查要删除的图片路径
            let querySQL = """
                SELECT id, image_path FROM clipboard_items
                WHERE is_pinned = 0 AND created_at < ?
            """
            var stmt: OpaquePointer?
            guard sqlite3_prepare_v2(db, querySQL, -1, &stmt, nil) == SQLITE_OK else {
                return 0
            }

            sqlite3_bind_text(stmt, 1, (cutoffStr as NSString).utf8String, -1, nil)

            // 收集要删除的图片路径
            var imagePaths: [String] = []
            while sqlite3_step(stmt) == SQLITE_ROW {
                if let pathCStr = sqlite3_column_text(stmt, 1) {
                    imagePaths.append(String(cString: pathCStr))
                }
            }
            sqlite3_finalize(stmt)

            // 删除磁盘文件
            for path in imagePaths {
                if FileManager.default.fileExists(atPath: path) {
                    try? FileManager.default.removeItem(atPath: path)
                }
            }

            // 删除数据库记录
            let deleteSQL = """
                DELETE FROM clipboard_items
                WHERE is_pinned = 0 AND created_at < ?
            """
            guard sqlite3_prepare_v2(db, deleteSQL, -1, &stmt, nil) == SQLITE_OK else {
                return 0
            }
            defer { sqlite3_finalize(stmt) }

            sqlite3_bind_text(stmt, 1, (cutoffStr as NSString).utf8String, -1, nil)
            guard sqlite3_step(stmt) == SQLITE_DONE else { return 0 }

            let deleted = Int(sqlite3_changes(db))
            if deleted > 0 {
                print("[DatabaseService] 清理了 \(deleted) 条过期记录")
            }
            return deleted
        }
    }
}
