using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ClipboardManager.Models;
using UserControl = System.Windows.Controls.UserControl;

namespace ClipboardManager.UI;

/// <summary>
/// 单条剪贴板记录卡片。
/// 与 Mac 版 ClipboardCardView 对应。
/// 通过 DataContext 绑定 ClipboardItem 数据。
/// </summary>
public partial class ClipboardCard : UserControl
{
    public ClipboardCard()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // 悬停效果
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;

        // 删除按钮悬停变色
        DeleteIcon.MouseEnter += (s, e) =>
            DeleteIcon.Foreground =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xCC, 0x33, 0x33));
        DeleteIcon.MouseLeave += (s, e) =>
            DeleteIcon.Foreground =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x55, 0x55, 0x55));
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not ClipboardItem item)
            return;

        RenderPreview(item);
        TimeLabel.Text = item.RelativeTime;
        UpdatePinState(item.IsPinned);
    }

    private void RenderPreview(ClipboardItem item)
    {
        switch (item.Type)
        {
            case ContentType.Text:
                TextPreview.Visibility = Visibility.Visible;
                ImagePreview.Visibility = Visibility.Collapsed;
                ImageLostText.Visibility = Visibility.Collapsed;

                var text = item.TextContent ?? string.Empty;
                // 截断显示（最多 300 字符，与 Mac 版一致）
                if (text.Length > 300)
                    text = string.Concat(text.AsSpan(0, 300), "...");
                TextPreview.Text = text;
                break;

            case ContentType.Image:
                TextPreview.Visibility = Visibility.Collapsed;

                if (!string.IsNullOrEmpty(item.ImagePath) &&
                    File.Exists(item.ImagePath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(item.ImagePath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.DecodePixelWidth = 330; // 限制解码大小，节省内存
                        bitmap.EndInit();
                        bitmap.Freeze();

                        ImagePreview.Source = bitmap;
                        ImagePreview.Visibility = Visibility.Visible;
                        ImageLostText.Visibility = Visibility.Collapsed;
                    }
                    catch
                    {
                        ImagePreview.Visibility = Visibility.Collapsed;
                        ImageLostText.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    ImagePreview.Visibility = Visibility.Collapsed;
                    ImageLostText.Visibility = Visibility.Visible;
                }
                break;
        }
    }

    private void UpdatePinState(bool isPinned)
    {
        if (isPinned)
        {
            PinBar.Visibility = Visibility.Visible;
            PinIcon.Text = "📌";
            PinIcon.Foreground =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x1A));
        }
        else
        {
            PinBar.Visibility = Visibility.Collapsed;
            PinIcon.Text = "📌";
            PinIcon.Foreground =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x88, 0x88, 0x88));
        }
    }

    // ── 悬停效果 ──────────────────────────────────

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (DataContext is ClipboardItem item && item.IsPinned)
        {
            CardRoot.Background =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xED, 0xF1, 0xF8));
        }
        else
        {
            CardRoot.Background =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xF5, 0xF5, 0xF5));
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (DataContext is ClipboardItem item && item.IsPinned)
        {
            CardRoot.Background =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xF5, 0xF8, 0xFC));
        }
        else
        {
            CardRoot.Background =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
        }
    }

    // ── 按钮操作 ──────────────────────────────────

    private void OnPinClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ClipboardItem item)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.TogglePin(item.Id);
        }
        e.Handled = true; // 阻止事件冒泡引发卡片复制
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ClipboardItem item)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.DeleteItem(item.Id);
        }
        e.Handled = true; // 阻止事件冒泡引发卡片复制
    }
}
