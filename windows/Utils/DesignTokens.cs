using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace ClipboardManager.Utils;

/// <summary>
/// 设计令牌：颜色、尺寸常量。
/// 与 Mac 版 Extensions.swift 中的 Color + DesignMetrics 完全一致。
/// 所有 UI 组件引用此处定义，确保视觉一致性。
/// </summary>
public static class DesignTokens
{
    // ── 尺寸 Metrics（与 Mac DesignMetrics 完全一致）──

    public const double PanelWidth = 380;
    public const double PanelHeight = 520;
    public const double CardPadding = 8;
    public const double CardGap = 6;
    public const double CardRadius = 6;
    public const double ThumbMaxWidth = 330;
    public const double ThumbMaxHeight = 140;
    public const double ThumbMinDisplayHeight = 36;
    public const double PreviewFontSize = 13;
    public const double TimeFontSize = 12;
    public const double ButtonSize = 12;
    public const double SearchHeight = 36;
    public const double BottomBarHeight = 20;
    public const int PreviewLineLimit = 8;
    public const int PreviewMaxChars = 300;

    // ── 颜色 Palette（与 Mac Extensions.swift 完全一致）──

    /// <summary>面板背景 #F0F0F0</summary>
    public static readonly Color PanelBg = Color.FromRgb(0xF0, 0xF0, 0xF0);

    /// <summary>卡片背景 #FFFFFF</summary>
    public static readonly Color CardBg = Color.FromRgb(0xFF, 0xFF, 0xFF);

    /// <summary>卡片悬停 #F5F5F5</summary>
    public static readonly Color CardHover = Color.FromRgb(0xF5, 0xF5, 0xF5);

    /// <summary>卡片边框 #E0E0E0</summary>
    public static readonly Color CardBorder = Color.FromRgb(0xE0, 0xE0, 0xE0);

    /// <summary>主文字 #1A1A1A</summary>
    public static readonly Color TextPrimary = Color.FromRgb(0x1A, 0x1A, 0x1A);

    /// <summary>次要文字 #333333</summary>
    public static readonly Color TextSecondary = Color.FromRgb(0x33, 0x33, 0x33);

    /// <summary>置顶标识 #4A90D9</summary>
    public static readonly Color PinBlue = Color.FromRgb(0x4A, 0x90, 0xD9);

    /// <summary>置顶卡片底 #F5F8FC</summary>
    public static readonly Color PinBg = Color.FromRgb(0xF5, 0xF8, 0xFC);

    /// <summary>置顶卡片悬停 #EDF1F8</summary>
    public static readonly Color PinHover = Color.FromRgb(0xED, 0xF1, 0xF8);

    /// <summary>搜索框聚焦 #4A90D9</summary>
    public static readonly Color SearchFocus = Color.FromRgb(0x4A, 0x90, 0xD9);

    /// <summary>删除按钮默认 #555555</summary>
    public static readonly Color DeleteInactive = Color.FromRgb(0x55, 0x55, 0x55);

    /// <summary>删除按钮悬停 #CC3333</summary>
    public static readonly Color DeleteHover = Color.FromRgb(0xCC, 0x33, 0x33);

    /// <summary>齿轮/状态文字 #999999</summary>
    public static readonly Color StatusText = Color.FromRgb(0x99, 0x99, 0x99);

    /// <summary>空状态文字 #BBBBBB</summary>
    public static readonly Color EmptyText = Color.FromRgb(0xBB, 0xBB, 0xBB);

    // ── Brush 缓存（避免重复创建）──

    public static readonly SolidColorBrush PanelBgBrush = new(PanelBg);
    public static readonly SolidColorBrush CardBgBrush = new(CardBg);
    public static readonly SolidColorBrush CardHoverBrush = new(CardHover);
    public static readonly SolidColorBrush CardBorderBrush = new(CardBorder);
    public static readonly SolidColorBrush TextPrimaryBrush = new(TextPrimary);
    public static readonly SolidColorBrush TextSecondaryBrush = new(TextSecondary);
    public static readonly SolidColorBrush PinBlueBrush = new(PinBlue);
    public static readonly SolidColorBrush PinBgBrush = new(PinBg);
    public static readonly SolidColorBrush PinHoverBrush = new(PinHover);
    public static readonly SolidColorBrush DeleteInactiveBrush = new(DeleteInactive);
    public static readonly SolidColorBrush DeleteHoverBrush = new(DeleteHover);
    public static readonly SolidColorBrush StatusTextBrush = new(StatusText);
    public static readonly SolidColorBrush EmptyTextBrush = new(EmptyText);
    public static readonly SolidColorBrush WhiteBrush = new(Colors.White);
    public static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);
}
