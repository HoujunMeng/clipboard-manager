import SwiftUI

// MARK: — 设计规范颜色

extension Color {
    /// 面板背景 #F0F0F0
    static let panelBg = Color(red: 0.941, green: 0.941, blue: 0.941)
    /// 卡片背景 #FFFFFF
    static let cardBg = Color.white
    /// 卡片悬停 #F5F5F5
    static let cardHover = Color(red: 0.961, green: 0.961, blue: 0.961)
    /// 卡片边框 #E0E0E0
    static let cardBorder = Color(red: 0.878, green: 0.878, blue: 0.878)
    /// 主文字 #333333 → 实际用 #1A1A1A（更清晰）
    static let textPrimary = Color(red: 0.102, green: 0.102, blue: 0.102)
    /// 次要文字 #999999 → 实际用 #333333（更清晰）
    static let textSecondary = Color(red: 0.2, green: 0.2, blue: 0.2)
    /// 置顶标识 #4A90D9
    static let pinBlue = Color(red: 0.29, green: 0.565, blue: 0.851)
    /// 置顶卡片底 #F5F8FC
    static let pinBg = Color(red: 0.961, green: 0.973, blue: 0.988)
    /// 搜索框聚焦 #4A90D9
    static let searchFocus = Color(red: 0.29, green: 0.565, blue: 0.851)
    /// 删除按钮默认 #555555
    static let deleteInactive = Color(red: 0.333, green: 0.333, blue: 0.333)
    /// 删除按钮悬停 #CC3333
    static let deleteHover = Color(red: 0.8, green: 0.2, blue: 0.2)
    /// 齿轮/状态文字 #999999
    static let statusText = Color(red: 0.6, green: 0.6, blue: 0.6)
    /// 空状态文字 #BBBBBB
    static let emptyText = Color(red: 0.733, green: 0.733, blue: 0.733)
}

// MARK: — 设计规范尺寸

enum DesignMetrics {
    static let panelWidth: CGFloat = 380
    static let panelHeight: CGFloat = 520
    static let cardPadding: CGFloat = 8
    static let cardGap: CGFloat = 6
    static let cardRadius: CGFloat = 6
    static let thumbMaxWidth: CGFloat = 330
    static let thumbMaxHeight: CGFloat = 140
    static let thumbMinDisplayHeight: CGFloat = 36
    static let previewFontSize: CGFloat = 13
    static let timeFontSize: CGFloat = 12
    static let buttonSize: CGFloat = 12
    static let searchHeight: CGFloat = 36
    static let bottomBarHeight: CGFloat = 20
}

// MARK: — String 扩展

extension String {
    /// 截取显示用文字（最多 80 字符）
    var truncatedForDisplay: String {
        let singleLine = self.replacingOccurrences(of: "\n", with: " ")
                             .replacingOccurrences(of: "\r", with: " ")
        if singleLine.count > 80 {
            return String(singleLine.prefix(80)) + "..."
        }
        return singleLine
    }
}
