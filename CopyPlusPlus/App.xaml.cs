using System.Windows;

namespace WpfMultiCopyClipboard;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // This app is launched in two ways:
        // 1. Normal launch: opens the clipboard manager window.
        // 2. Windows context menu item "copy++": also opens the same window.
        //
        // Keep command-line handling here so you can add actions later, for example:
        // --paste-all, --clear, --register, etc.
        if (e.Args.Any(a => a.Equals("--help", StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(
                "copy++ clipboard manager\n\nCtrl+C twice: save current selection\nCtrl+V twice: paste all saved items",
                "copy++");
            Shutdown();
        }
    }
}
