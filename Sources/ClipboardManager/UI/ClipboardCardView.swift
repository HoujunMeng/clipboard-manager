import SwiftUI

/// 单条剪贴板记录卡片
struct ClipboardCardView: View {
    let item: ClipboardItem
    let onCopy: (Int) -> Void
    let onPin: (Int) -> Void
    let onDelete: (Int) -> Void

    @State private var isHovered = false
    @State private var deleteHovered = false

    var body: some View {
        HStack(spacing: 0) {
            // 置顶蓝色标识
            if item.isPinned {
                RoundedRectangle(cornerRadius: 1)
                    .fill(Color.pinBlue)
                    .frame(width: 3)
                    .padding(.vertical, -DesignMetrics.cardPadding)
            }

            // 主内容区
            VStack(spacing: 4) {
                // 预览区域
                previewContent

                // 底部栏
                bottomBar
            }
            .padding(DesignMetrics.cardPadding)
        }
        .background(cardBackground)
        .overlay(
            RoundedRectangle(cornerRadius: DesignMetrics.cardRadius)
                .stroke(Color.cardBorder, lineWidth: 1)
        )
        .clipShape(RoundedRectangle(cornerRadius: DesignMetrics.cardRadius))
        .onHover { hovering in
            withAnimation(.easeInOut(duration: 0.1)) {
                isHovered = hovering
            }
        }
        .onTapGesture {
            onCopy(item.id)
        }
    }

    // MARK: — 背景色

    private var cardBackground: some View {
        RoundedRectangle(cornerRadius: DesignMetrics.cardRadius)
            .fill(backgroundColor)
    }

    private var backgroundColor: Color {
        if item.isPinned {
            return isHovered ? Color(red: 0.929, green: 0.945, blue: 0.973) : Color.pinBg
        } else {
            return isHovered ? Color.cardHover : Color.cardBg
        }
    }

    // MARK: — 预览区域

    @ViewBuilder
    private var previewContent: some View {
        switch item.contentType {
        case .text:
            textPreview
        case .image:
            imagePreview
        }
    }

    private var textPreview: some View {
        Text(item.textContent?.truncatedForDisplay ?? "[空]")
            .font(.system(size: DesignMetrics.previewFontSize))
            .foregroundColor(.textPrimary)
            .lineLimit(2)
            .lineSpacing(2)
            .frame(maxWidth: .infinity, alignment: .leading)
    }

    @ViewBuilder
    private var imagePreview: some View {
        if let imagePath = item.imagePath,
           FileManager.default.fileExists(atPath: imagePath),
           let nsImage = NSImage(contentsOfFile: imagePath) {
            Image(nsImage: nsImage)
                .resizable()
                .aspectRatio(contentMode: .fit)
                .frame(
                    maxWidth: DesignMetrics.thumbMaxWidth,
                    minHeight: DesignMetrics.thumbMinDisplayHeight,
                    maxHeight: DesignMetrics.thumbMaxHeight
                )
                .clipShape(RoundedRectangle(cornerRadius: 4))
        } else {
            Text("[图片已丢失]")
                .font(.system(size: DesignMetrics.previewFontSize))
                .foregroundColor(.textSecondary)
                .frame(maxWidth: .infinity, alignment: .leading)
        }
    }

    // MARK: — 底部栏

    private var bottomBar: some View {
        HStack(spacing: 12) {
            // 相对时间
            Text(item.relativeTime)
                .font(.system(size: DesignMetrics.timeFontSize))
                .foregroundColor(.textSecondary)

            Spacer()

            // 置顶按钮（图钉图标）
            pinButton

            // 删除按钮（叉号图标）
            deleteButton
        }
        .frame(height: DesignMetrics.bottomBarHeight)
    }

    // MARK: — 按钮

    private var pinButton: some View {
        Button(action: { onPin(item.id) }) {
            pinIcon
                .frame(width: DesignMetrics.buttonSize,
                       height: DesignMetrics.buttonSize)
        }
        .buttonStyle(.plain)
        .help(item.isPinned ? "取消置顶" : "置顶")
    }

    private var pinIcon: some View {
        Image(systemName: item.isPinned ? "pin.fill" : "pin")
            .font(.system(size: 12))
            .foregroundColor(item.isPinned ? .textPrimary : Color(red: 0.533, green: 0.533, blue: 0.533))
    }

    private var deleteButton: some View {
        Button(action: { onDelete(item.id) }) {
            deleteIcon
                .frame(width: DesignMetrics.buttonSize,
                       height: DesignMetrics.buttonSize)
        }
        .buttonStyle(.plain)
        .help("删除")
        .onHover { hovering in
            deleteHovered = hovering
        }
    }

    private var deleteIcon: some View {
        let color: Color = deleteHovered ? .deleteHover : .deleteInactive

        return Canvas { context, size in
            let margin: CGFloat = size.width * 0.3
            let path = Path { p in
                p.move(to: CGPoint(x: margin, y: margin))
                p.addLine(to: CGPoint(x: size.width - margin, y: size.height - margin))
                p.move(to: CGPoint(x: size.width - margin, y: margin))
                p.addLine(to: CGPoint(x: margin, y: size.height - margin))
            }
            context.stroke(path, with: .color(color),
                           style: StrokeStyle(lineWidth: 2, lineCap: .round))
        }
    }
}
