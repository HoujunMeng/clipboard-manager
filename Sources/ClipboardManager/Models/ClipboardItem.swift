import Foundation

/// 剪贴板记录数据模型
struct ClipboardItem: Identifiable, Equatable {
    let id: Int
    let contentType: ContentType
    let textContent: String?
    let imagePath: String?
    let createdAt: Date
    let isPinned: Bool

    enum ContentType: String, Equatable {
        case text
        case image
    }

    /// 文字预览（最多 100 字）
    var preview: String {
        switch contentType {
        case .text:
            guard let text = textContent else { return "[空]" }
            let singleLine = text.replacingOccurrences(of: "\n", with: " ")
                                 .replacingOccurrences(of: "\r", with: " ")
            if singleLine.count > 100 {
                return String(singleLine.prefix(100)) + "..."
            }
            return singleLine
        case .image:
            return "[图片]"
        }
    }

    /// 相对时间描述（与 Python 版逻辑一致）
    var relativeTime: String {
        let now = Date()
        let diff = now.timeIntervalSince(createdAt)

        switch diff {
        case ..<60:
            return "刚刚"
        case ..<3600:
            let mins = Int(diff / 60)
            return "\(mins)分钟前"
        case ..<86400:
            let hours = Int(diff / 3600)
            return "\(hours)小时前"
        case ..<172800:
            let formatter = DateFormatter()
            formatter.dateFormat = "HH:mm"
            return "昨天 \(formatter.string(from: createdAt))"
        case ..<604800:
            let days = Int(diff / 86400)
            return "\(days)天前"
        default:
            let formatter = DateFormatter()
            formatter.dateFormat = "MM月dd日 HH:mm"
            return formatter.string(from: createdAt)
        }
    }
}
