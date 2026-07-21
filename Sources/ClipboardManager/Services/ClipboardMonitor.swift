import AppKit
import CryptoKit

/// 剪贴板监控服务：500ms 轮询，检测文字/图片变动
final class ClipboardMonitor {
    static let shared = ClipboardMonitor()

    private let pasteboard = NSPasteboard.general
    private var lastChangeCount: Int = 0
    private var lastTextHash: String = ""
    private var lastImageHash: String = ""
    private var timer: Timer?
    private var ignoreNextChange = false
    private var saveCount = 0

    /// 当新记录添加时发送通知
    static let newItemNotification = Notification.Name("ClipboardMonitorNewItem")

    private init() {}

    // MARK: — 启动 / 停止

    func start() {
        lastChangeCount = pasteboard.changeCount
        // 初始化当前剪贴板状态
        updateLastState()

        timer = Timer.scheduledTimer(withTimeInterval: 0.5, repeats: true) { [weak self] _ in
            self?.checkClipboard()
        }
        // 允许 timer 在 runloop 常用模式下也触发
        if let timer = timer {
            RunLoop.current.add(timer, forMode: .common)
        }
        print("[ClipboardMonitor] 监控已启动 (500ms 轮询)")
    }

    func stop() {
        timer?.invalidate()
        timer = nil
        print("[ClipboardMonitor] 监控已停止")
    }

    /// 标记下一次变动忽略（从本应用复制回剪贴板时调用）
    func ignoreOnce() {
        ignoreNextChange = true
    }

    // MARK: — 检测

    private func updateLastState() {
        guard let items = pasteboard.pasteboardItems, let item = items.first else {
            lastTextHash = ""
            lastImageHash = ""
            return
        }

        if let text = item.string(forType: .string) {
            lastTextHash = hashText(text)
            lastImageHash = ""
        } else if let imageData = item.data(forType: .tiff) ??
                              item.data(forType: .png) {
            if let image = NSImage(data: imageData) {
                lastTextHash = ""
                lastImageHash = hashImage(image)
            }
        }
    }

    private func checkClipboard() {
        let currentChangeCount = pasteboard.changeCount

        // changeCount 没变，跳过
        guard currentChangeCount != lastChangeCount else { return }
        lastChangeCount = currentChangeCount

        // 忽略自触发变动
        if ignoreNextChange {
            ignoreNextChange = false
            updateLastState()
            return
        }

        guard let items = pasteboard.pasteboardItems, let item = items.first else {
            return
        }

        // 1. 检测图片（优先检查，因为图片可能也有字符串表示）
        if let image = readImage(from: item) {
            let hash = hashImage(image)
            guard hash != lastImageHash, !hash.isEmpty else { return }
            lastImageHash = hash
            lastTextHash = ""
            saveImage(image)
            return
        }

        // 2. 检测文件 URL（Finder 复制的图片文件）
        if let fileURL = readImageFileURL(from: item) {
            if let image = NSImage(contentsOf: fileURL) {
                let hash = hashImage(image)
                guard hash != lastImageHash, !hash.isEmpty else { return }
                lastImageHash = hash
                lastTextHash = ""
                saveImageFromFile(fileURL)
                return
            }
        }

        // 3. 检测文字
        if let text = item.string(forType: .string), !text.isEmpty {
            let hash = hashText(text)
            guard hash != lastTextHash else { return }
            lastTextHash = hash
            lastImageHash = ""
            saveText(text, hash: hash)
        }
    }

    // MARK: — 读取

    private func readImage(from item: NSPasteboardItem) -> NSImage? {
        // 尝试各种图片格式
        for type in [NSPasteboard.PasteboardType.tiff,
                     NSPasteboard.PasteboardType.png] {
            if let data = item.data(forType: type),
               let image = NSImage(data: data), image.isValid {
                return image
            }
        }
        return nil
    }

    private func readImageFileURL(from item: NSPasteboardItem) -> URL? {
        // 从 Finder 复制的文件会包含 fileURL
        guard let fileURLStr = item.string(forType: .fileURL) else {
            return nil
        }
        // 去掉换行符（多个文件时用换行分隔）
        let urlStr = fileURLStr.components(separatedBy: "\n").first ?? fileURLStr
        guard let url = URL(string: urlStr) else { return nil }

        let imageExtensions = ["png", "jpg", "jpeg", "gif", "bmp", "webp",
                                "tiff", "tif", "heic", "heif"]
        guard imageExtensions.contains(url.pathExtension.lowercased()) else {
            return nil
        }
        guard FileManager.default.fileExists(atPath: url.path) else {
            return nil
        }
        return url
    }

    // MARK: — 保存

    private func saveText(_ text: String, hash: String) {
        let itemId = DatabaseService.shared.saveItem(
            contentType: .text,
            textContent: text,
            textHash: hash
        )
        if itemId != nil {
            maybeCleanup()
            notifyNewItem()
            print("[ClipboardMonitor] 文字已保存: \(String(text.prefix(50)).truncatedForDisplay)")
        }
    }

    private func saveImage(_ image: NSImage) {
        guard let filepath = writeImageToDisk(image) else { return }

        let itemId = DatabaseService.shared.saveItem(
            contentType: .image,
            imagePath: filepath
        )
        if itemId != nil {
            maybeCleanup()
            notifyNewItem()
            print("[ClipboardMonitor] 图片已保存: \(filepath)")
        } else {
            // 保存失败，清理文件
            try? FileManager.default.removeItem(atPath: filepath)
        }
    }

    private func saveImageFromFile(_ fileURL: URL) {
        // 直接复制文件（保证完整画质）
        let imagesDir = DatabaseService.shared.imagesDir
        let timestamp = dateTimestamp()
        let ext = fileURL.pathExtension.lowercased()
        let filename = "\(timestamp).\(ext == "jpeg" ? "jpg" : ext)"
        let destPath = imagesDir + "/" + filename

        do {
            try FileManager.default.copyItem(at: fileURL, to: URL(fileURLWithPath: destPath))
        } catch {
            print("[ClipboardMonitor] 图片文件复制失败: \(error)")
            return
        }

        let itemId = DatabaseService.shared.saveItem(
            contentType: .image,
            imagePath: destPath
        )
        if itemId != nil {
            maybeCleanup()
            notifyNewItem()
            print("[ClipboardMonitor] 图片已保存(文件): \(destPath)")
        } else {
            try? FileManager.default.removeItem(atPath: destPath)
        }
    }

    private func writeImageToDisk(_ image: NSImage) -> String? {
        let imagesDir = DatabaseService.shared.imagesDir
        let timestamp = dateTimestamp()
        let filepath = imagesDir + "/\(timestamp).png"

        // NSImage → CGImage → PNG Data
        guard let cgImage = image.cgImage(forProposedRect: nil, context: nil, hints: nil) else {
            print("[ClipboardMonitor] 无法获取 CGImage")
            return nil
        }

        let bitmapRep = NSBitmapImageRep(cgImage: cgImage)
        guard let pngData = bitmapRep.representation(using: .png, properties: [:]) else {
            print("[ClipboardMonitor] PNG 编码失败")
            return nil
        }

        do {
            try pngData.write(to: URL(fileURLWithPath: filepath))
        } catch {
            print("[ClipboardMonitor] 图片写入失败: \(error)")
            return nil
        }

        // 验证文件
        guard FileManager.default.fileExists(atPath: filepath),
              let attrs = try? FileManager.default.attributesOfItem(atPath: filepath),
              let fileSize = attrs[.size] as? Int, fileSize > 0 else {
            print("[ClipboardMonitor] 图片文件无效")
            return nil
        }

        return filepath
    }

    // MARK: — 哈希（去重）

    private func hashText(_ text: String) -> String {
        guard let data = text.data(using: .utf8) else { return "" }
        return SHA256.hash(data: data).compactMap { String(format: "%02x", $0) }.joined()
    }

    private func hashImage(_ image: NSImage) -> String {
        // 缩放到 32×32 以减小哈希计算量
        guard let smallImage = image.resized(to: NSSize(width: 32, height: 32)),
              let tiffData = smallImage.tiffRepresentation,
              let bitmapRep = NSBitmapImageRep(data: tiffData),
              let pngData = bitmapRep.representation(using: .png, properties: [:]) else {
            return ""
        }
        return SHA256.hash(data: pngData).compactMap { String(format: "%02x", $0) }.joined()
    }

    // MARK: — 清理与通知

    private func maybeCleanup() {
        saveCount += 1
        guard saveCount >= 50 else { return }
        saveCount = 0
        let days = SettingsManager.shared.retentionDays
        DatabaseService.shared.cleanup(retentionDays: days)
    }

    private func notifyNewItem() {
        DispatchQueue.main.async {
            NotificationCenter.default.post(
                name: Self.newItemNotification, object: nil)
        }
    }

    private func dateTimestamp() -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyyMMdd_HHmmss_SSSSSS"
        return formatter.string(from: Date())
    }
}

// MARK: — NSImage 缩放扩展

extension NSImage {
    func resized(to targetSize: NSSize) -> NSImage? {
        guard isValid, let rep = bestRepresentation(
            for: NSRect(x: 0, y: 0, width: targetSize.width, height: targetSize.height),
            context: nil, hints: nil) else {
            return nil
        }

        let origSize = NSSize(width: rep.pixelsWide, height: rep.pixelsHigh)
        let scale = min(targetSize.width / origSize.width,
                        targetSize.height / origSize.height)
        let newSize = NSSize(width: origSize.width * scale,
                             height: origSize.height * scale)

        let result = NSImage(size: newSize)
        result.lockFocus()
        NSGraphicsContext.current?.imageInterpolation = .high
        rep.draw(in: NSRect(origin: .zero, size: newSize))
        result.unlockFocus()
        return result
    }
}
