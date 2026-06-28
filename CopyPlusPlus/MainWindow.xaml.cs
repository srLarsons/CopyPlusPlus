using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace WpfMultiCopyClipboard;

public partial class MainWindow : Window
{
    private const int CopyHotkeyId = 1;
    private const int PasteHotkeyId = 2;
    private readonly TimeSpan _doublePressWindow = TimeSpan.FromMilliseconds(650);
    private DateTime _lastCopyPress = DateTime.MinValue;
    private DateTime _lastPastePress = DateTime.MinValue;
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

        RegisterHotkeyOrThrow(handle, CopyHotkeyId, NativeMethods.MOD_CONTROL, NativeMethods.KEY_C);
        RegisterHotkeyOrThrow(handle, PasteHotkeyId, NativeMethods.MOD_CONTROL, NativeMethods.KEY_V);
    }

    protected override void OnClosed(EventArgs e)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        NativeMethods.UnregisterHotKey(handle, CopyHotkeyId);
        NativeMethods.UnregisterHotKey(handle, PasteHotkeyId);
        _source?.RemoveHook(WndProc);
        base.OnClosed(e);
    }

    private static void RegisterHotkeyOrThrow(IntPtr handle, int id, uint modifiers, uint key)
    {
        if (!NativeMethods.RegisterHotKey(handle, id, modifiers, key))
        {
            int error = Marshal.GetLastWin32Error();
            MessageBox.Show($"Could not register global hotkey. Error: {error}", "Hotkey error");
        }
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
        else if (id == PasteHotkeyId)
        {
            HandlePasteHotkey();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void HandleCopyHotkey()
    {
        DateTime now = DateTime.Now;
        bool isDoublePress = now - _lastCopyPress <= _doublePressWindow;
        _lastCopyPress = now;

        TemporarilySendKeys(SendCtrlCToActiveApp);

        if (!isDoublePress)
            return;

        // Give the active app a moment to put selected data on the clipboard.
        Dispatcher.BeginInvoke(async () =>
        {
            await Task.Delay(200);
            AddCurrentClipboardToList();
        });
    }

    private void HandlePasteHotkey()
    {
        DateTime now = DateTime.Now;
        bool isDoublePress = now - _lastPastePress <= _doublePressWindow;
        _lastPastePress = now;

        if (!isDoublePress)
        {
            TemporarilySendKeys(SendCtrlVToActiveApp);
            return;
        }

        PutAllItemsOnClipboard();
        Dispatcher.BeginInvoke(async () =>
        {
            await Task.Delay(80);
            TemporarilySendKeys(SendCtrlVToActiveApp);
        });
    }

    private void TemporarilySendKeys(Action sendKeys)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        NativeMethods.UnregisterHotKey(handle, CopyHotkeyId);
        NativeMethods.UnregisterHotKey(handle, PasteHotkeyId);

        sendKeys();

        Dispatcher.BeginInvoke(async () =>
        {
            await Task.Delay(120);
            NativeMethods.RegisterHotKey(handle, CopyHotkeyId, NativeMethods.MOD_CONTROL, NativeMethods.KEY_C);
            NativeMethods.RegisterHotKey(handle, PasteHotkeyId, NativeMethods.MOD_CONTROL, NativeMethods.KEY_V);
        });
    }

    private void AddCurrentClipboardToList()
    {
        try
        {
            var item = ClipboardItem.FromCurrentClipboard();
            if (item.Kind != ClipboardItemKind.Unknown)
                Items.Add(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Copy failed");
        }
    }

    private void PutAllItemsOnClipboard()
    {
        if (Items.Count == 0)
            return;

        // Best combined result: text and file paths are joined into one text block.
        // If there is only one image, the image is preserved as an image clipboard item.
        if (Items.Count == 1 && Items[0].Kind == ClipboardItemKind.Image && Items[0].Image is BitmapSource singleImage)
        {
            Clipboard.SetImage(singleImage);
            return;
        }

        var parts = new List<string>();
        int imageNumber = 1;

        foreach (var item in Items)
        {
            switch (item.Kind)
            {
                case ClipboardItemKind.Text when item.Text is not null:
                    parts.Add(item.Text);
                    break;
                case ClipboardItemKind.Files when item.Files is not null:
                    parts.Add(string.Join(Environment.NewLine, item.Files));
                    break;
                case ClipboardItemKind.Image:
                    parts.Add($"[Image {imageNumber++} copied in list]");
                    break;
            }
        }

        Clipboard.SetText(string.Join(Environment.NewLine + Environment.NewLine, parts));
    }

    private void PasteAll_Click(object sender, RoutedEventArgs e)
    {
        PutAllItemsOnClipboard();
        MessageBox.Show("All saved items are now on the clipboard. Press Ctrl+V where you want to paste.", "Ready to paste");
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        Items.Clear();
    }

    private static void SendCtrlCToActiveApp()
    {
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_C, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_C, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static void SendCtrlVToActiveApp()
    {
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_V, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_V, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
