using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;

namespace WpfMultiCopyClipboard;

public partial class App : Application
{
    private const string MutexName = "Local\\CopyPlusPlus.SingleInstance";
    private const string PipeName = "CopyPlusPlus.AppCommand";

    private Mutex? _mutex;
    private CancellationTokenSource? _pipeCancellation;
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, MutexName, out bool ownsMutex);
        if (!ownsMutex)
        {
            SendCommand("show");
            Shutdown();
            return;
        }

        _window = new MainWindow();
        MainWindow = _window;
        StartCommandServer();

        if (HasArg(e.Args, "--help"))
        {
            MessageBox.Show(
                "copy++ clipboard manager\n\nCtrl+C twice: save current selection\nCtrl+V twice: paste all saved items",
                "copy++");
            Shutdown();
            return;
        }

        if (HasArg(e.Args, "--minimized"))
            _window.WindowState = WindowState.Minimized;

        _window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _pipeCancellation?.Cancel();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private void StartCommandServer()
    {
        _pipeCancellation = new CancellationTokenSource();
        _ = ListenForCommandsAsync(_pipeCancellation.Token);
    }

    private async Task ListenForCommandsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await pipe.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(pipe);
                string? command = await reader.ReadLineAsync(cancellationToken);

                if (string.Equals(command, "show", StringComparison.OrdinalIgnoreCase))
                    await Dispatcher.InvokeAsync(ShowMainWindow);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
            }
        }
    }

    private void ShowMainWindow()
    {
        if (_window is null)
            return;

        if (!_window.IsVisible)
            _window.Show();

        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private static void SendCommand(string command)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            pipe.Connect(600);
            using var writer = new StreamWriter(pipe) { AutoFlush = true };
            writer.WriteLine(command);
        }
        catch
        {
        }
    }

    private static bool HasArg(IEnumerable<string> args, string name)
    {
        return args.Any(arg => arg.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
