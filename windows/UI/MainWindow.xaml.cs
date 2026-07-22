using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClipboardManager.Models;
using ClipboardManager.Services;
using Clipboard = System.Windows.Clipboard;

namespace ClipboardManager.UI;

/// <summary>
/// 主弹出面板（完整 UI）。
/// 与 Mac 版 HistoryPanelView + HistoryPanelViewModel 对应。
/// </summary>
public partial class MainWindow : Window
{
    private List<ClipboardItem> _items = [];
    private string _searchText = string.Empty;

    public MainWindow()
    {
        InitializeComponent();

        // 订阅剪贴板监控的新记录事件
        ClipboardMonitor.Instance.NewItemAdded += OnNewItemFromMonitor;

        // 窗口失焦时自动关闭（与 Mac transient 行为一致）
        Deactivated += OnDeactivated;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // 防止真正关闭窗口（只隐藏，复用以保留状态）
        e.Cancel = true;
        Hide();
    }

    // ── 数据刷新 ──────────────────────────────────

    public void RefreshData()
    {
        _items = DatabaseService.Instance.GetItems(_searchText);
        RenderCards();
        UpdateStatusBar();
    }

    private void RenderCards()
    {
        if (_items.Count == 0)
        {
            CardList.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                EmptyIcon.Text = "📋";
                EmptyText.Text = "暂无复制记录";
            }
            else
            {
                EmptyIcon.Text = "🔍";
                EmptyText.Text = "无匹配记录";
            }
        }
        else
        {
            CardList.Visibility = Visibility.Visible;
            EmptyState.Visibility = Visibility.Collapsed;
            CardList.ItemsSource = _items;
        }
    }

    private void UpdateStatusBar()
    {
        var total = DatabaseService.Instance.GetTotalCount();
        var days = SettingsManager.Instance.RetentionDays;
        StatusLabel.Text = $"共 {total} 条记录 · 保留 {days} 天";
    }

    // ── 搜索 ──────────────────────────────────────

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text.Trim();

        // 更新占位文字和清除按钮可见性
        PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible : Visibility.Collapsed;
        ClearButton.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Collapsed : Visibility.Visible;

        RefreshData();
    }

    private void OnClearSearch(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        _searchText = string.Empty;
        RefreshData();
    }

    // ── 搜索框焦点 ────────────────────────────────

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        // 面板显示时自动聚焦搜索框
        SearchBox.Focus();
    }

    // ── 卡片操作 ──────────────────────────────────

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ClipboardCard card && card.DataContext is ClipboardItem item)
        {
            CopyItem(item.Id);
        }
    }

    private void CopyItem(int id)
    {
        var item = DatabaseService.Instance.GetItem(id);
        if (item == null) return;

        try
        {
            ClipboardMonitor.Instance.IgnoreOnce();

            switch (item.Type)
            {
                case ContentType.Text:
                    if (!string.IsNullOrEmpty(item.TextContent))
                        Clipboard.SetText(item.TextContent);
                    break;

                case ContentType.Image:
                    if (!string.IsNullOrEmpty(item.ImagePath) &&
                        File.Exists(item.ImagePath))
                    {
                        // 将图片文件路径放入剪贴板
                        var fileList = new System.Collections.Specialized.StringCollection
                        {
                            item.ImagePath
                        };
                        Clipboard.SetFileDropList(fileList);
                    }
                    break;
            }

            // 复制后关闭面板（与 Mac 版一致）
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsVisible) Hide();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[MainWindow] 复制失败: {ex.Message}");
        }
    }

    public void TogglePin(int id)
    {
        DatabaseService.Instance.TogglePin(id);
        RefreshData();
    }

    public void DeleteItem(int id)
    {
        DatabaseService.Instance.DeleteItem(id);
        RefreshData();
    }

    // ── 设置 ──────────────────────────────────────

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        Hide();
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var settingsWin = new SettingsWindow
            {
                Owner = this
            };
            settingsWin.ShowDialog();
            RefreshData(); // 设置可能改变了保留天数
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    // ── 面板关闭 ──────────────────────────────────

    private bool _isClosing;

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (!_isClosing && IsVisible)
        {
            Hide();
        }
    }

    public new void Hide()
    {
        _isClosing = true;
        try
        {
            base.Hide();
        }
        finally
        {
            _isClosing = false;
        }
    }

    // ── 剪贴板事件 ────────────────────────────────

    private void OnNewItemFromMonitor()
    {
        // 从后台线程切换到 UI 线程
        Dispatcher.Invoke(() =>
        {
            // 仅在面板可见时刷新（优化性能）
            if (IsVisible)
                RefreshData();
        });
    }
}
