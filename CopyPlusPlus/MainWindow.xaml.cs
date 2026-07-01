using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfMultiCopyClipboard;

public partial class MainWindow : Window
{
    private const int CopyHotkeyId = 1;
    private const int PasteAllHotkeyId = 2;
    private const int MaxSavedItems = 100;
    private const int MaxPasteMenuItems = 15;
    private const int PreviewMaxVisibleLines = 5;
    private const double PreviewLineHeight = 18;
    private HwndSource? _source;

    public ObservableCollection<ClipboardItem> Items { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        IntPtr handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);

        RegisterAppHotkeys(handle);
    }

    protected override void OnClosed(EventArgs e)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        UnregisterAppHotkeys(handle);
        _source?.RemoveHook(WndProc);
        base.OnClosed(e);
    }

    private static void RegisterHotkey(IntPtr handle, int id, uint modifiers, uint key)
    {
        NativeMethods.RegisterHotKey(handle, id, modifiers, key);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != NativeMethods.WM_HOTKEY)
            return IntPtr.Zero;

        int id = wParam.ToInt32();

        if (id == CopyHotkeyId)
        {
            HandleCopyHotkey();
            handled = true;
        }
        else if (id == PasteAllHotkeyId)
        {
            HandlePasteAllHotkey();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void HandleCopyHotkey()
    {
        Dispatcher.BeginInvoke(async () =>
        {
            await Task.Delay(120);
            TemporarilySendKeys(SendCtrlCToActiveApp);
            await Task.Delay(200);
            AddCurrentClipboardToList();
        });
    }

    private void HandlePasteAllHotkey()
    {
        if (!PutAllItemsOnClipboard())
            return;

        Dispatcher.BeginInvoke(async () =>
        {
            await Task.Delay(120);
            TemporarilySendKeys(SendCtrlVToActiveApp);
        });
    }

    private void TemporarilySendKeys(Action sendKeys)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        UnregisterAppHotkeys(handle);

        sendKeys();

        Dispatcher.BeginInvoke(async () =>
        {
            await Task.Delay(120);
            RegisterAppHotkeys(handle);
        });
    }

    private static void RegisterAppHotkeys(IntPtr handle)
    {
        const uint ctrlAlt = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT;
        RegisterHotkey(handle, CopyHotkeyId, ctrlAlt, NativeMethods.KEY_C);
        RegisterHotkey(handle, PasteAllHotkeyId, ctrlAlt, NativeMethods.KEY_V);
    }

    private static void UnregisterAppHotkeys(IntPtr handle)
    {
        NativeMethods.UnregisterHotKey(handle, CopyHotkeyId);
        NativeMethods.UnregisterHotKey(handle, PasteAllHotkeyId);
    }

    private void AddCurrentClipboardToList()
    {
        try
        {
            var item = ClipboardItem.FromCurrentClipboard();
            if (item.Kind != ClipboardItemKind.Unknown)
            {
                Items.Add(item);
                TrimSavedItems();
            }
        }
        catch
        {
        }
    }

    private void TrimSavedItems()
    {
        while (Items.Count > MaxSavedItems)
            Items.RemoveAt(0);
    }

    private bool PutAllItemsOnClipboard()
    {
        try
        {
            ClipboardWriter.PutAll(Items);
            return Items.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private void PasteItem(ClipboardItem item)
    {
        try
        {
            ClipboardWriter.PutItem(item);
            PasteClipboardToActiveTarget();
        }
        catch
        {
        }
    }

    private void DeleteItem(ClipboardItem item)
    {
        Items.Remove(item);
        RefreshPasteMenuItems();
    }

    private void PasteClipboardToActiveTarget()
    {
        Dispatcher.BeginInvoke(async () =>
        {
            WindowState = WindowState.Minimized;
            await Task.Delay(120);
            TemporarilySendKeys(SendCtrlVToActiveApp);
        });
    }

    private void ClipboardContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        RefreshPasteMenuItems();
    }

    private void RefreshPasteMenuItems()
    {
        PasteMenuItem.Items.Clear();
        DeleteMenuItem.Items.Clear();

        var items = Items.Reverse().Take(MaxPasteMenuItems).ToList();
        PasteMenuItem.IsEnabled = items.Count > 0;
        DeleteMenuItem.IsEnabled = items.Count > 0;

        if (items.Count == 0)
        {
            PasteMenuItem.Items.Add(new MenuItem
            {
                Header = "No copied items yet",
                IsEnabled = false
            });
            DeleteMenuItem.Items.Add(new MenuItem
            {
                Header = "No copied items yet",
                IsEnabled = false
            });
            return;
        }

        for (int index = 0; index < items.Count; index++)
        {
            ClipboardItem item = items[index];
            var pasteMenuItem = new MenuItem
            {
                Header = EscapeMenuHeader($"{index + 1}. {item.Title}: {GetMenuPreview(item)}"),
                ToolTip = CreateMenuToolTip(item)
            };
            pasteMenuItem.Click += (_, _) => PasteItem(item);
            PasteMenuItem.Items.Add(pasteMenuItem);

            var deleteMenuItem = new MenuItem
            {
                Header = EscapeMenuHeader($"{index + 1}. {item.Title}: {GetMenuPreview(item)}"),
                ToolTip = CreateMenuToolTip(item)
            };
            deleteMenuItem.Click += (_, _) => DeleteItem(item);
            DeleteMenuItem.Items.Add(deleteMenuItem);
        }
    }

    private static string GetMenuPreview(ClipboardItem item)
    {
        string preview = item.Kind switch
        {
            ClipboardItemKind.Text => item.Text ?? string.Empty,
            ClipboardItemKind.Files => item.Files is null ? item.Preview : string.Join(", ", item.Files.Select(Path.GetFileName)),
            ClipboardItemKind.Image => item.Preview,
            _ => item.Preview
        };

        preview = CollapseWhitespace(preview);
        return Truncate(string.IsNullOrWhiteSpace(preview) ? "(empty)" : preview, 90);
    }

    private static object CreateMenuToolTip(ClipboardItem item)
    {
        var panel = new StackPanel
        {
            MaxWidth = 440
        };

        panel.Children.Add(new TextBlock
        {
            Text = item.Title,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });

        if (item.Kind == ClipboardItemKind.Image && item.Image is BitmapSource image)
        {
            panel.Children.Add(CreateReadOnlyPreviewBox(item.Preview));
            panel.Children.Add(new Image
            {
                Source = image,
                Width = 260,
                Height = 150,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 6, 0, 0)
            });
            return panel;
        }

        string detail = item.Kind switch
        {
            ClipboardItemKind.Text => item.Text ?? string.Empty,
            ClipboardItemKind.Files => item.Files is null ? item.Preview : string.Join(Environment.NewLine, item.Files),
            _ => item.Preview
        };

        panel.Children.Add(CreateReadOnlyPreviewBox(detail));

        return panel;
    }

    private static TextBox CreateReadOnlyPreviewBox(string detail)
    {
        return new TextBox
        {
            Text = Truncate(string.IsNullOrWhiteSpace(detail) ? "(empty)" : detail, 3000),
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            MinWidth = 320,
            MaxWidth = 420,
            MaxHeight = PreviewLineHeight * PreviewMaxVisibleLines + 12,
            Padding = new Thickness(6, 4, 6, 4),
            BorderThickness = new Thickness(1),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }

    private static string CollapseWhitespace(string value)
    {
        return string.Join(" ", value.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }

    private static string EscapeMenuHeader(string value)
    {
        return value.Replace("_", "__");
    }

    private void PasteAll_Click(object sender, RoutedEventArgs e)
    {
        if (PutAllItemsOnClipboard())
            PasteClipboardToActiveTarget();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        Items.Clear();
    }

    private void PasteListItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: ClipboardItem item })
            PasteItem(item);
    }

    private void DeleteListItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: ClipboardItem item })
            DeleteItem(item);
    }

    private void ItemsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is ListBox { SelectedItem: ClipboardItem item })
            PasteItem(item);
    }

    private static void SendCtrlCToActiveApp()
    {
        ReleaseHotkeyModifiers();
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_C, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_C, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static void SendCtrlVToActiveApp()
    {
        ReleaseHotkeyModifiers();
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_V, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_V, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static void ReleaseHotkeyModifiers()
    {
        NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}