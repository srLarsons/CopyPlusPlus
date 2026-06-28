using System.IO;
using System.Windows.Media.Imaging;

namespace WpfMultiCopyClipboard;

public enum ClipboardItemKind
{
    Text,
    Image,
    Files,
    Unknown
}

public sealed class ClipboardItem
{
    public ClipboardItemKind Kind { get; init; }
    public string Title { get; init; } = "Clipboard item";
    public string Preview { get; init; } = "";
    public string? Text { get; init; }
    public BitmapSource? Image { get; init; }
    public string[]? Files { get; init; }

    public static ClipboardItem FromCurrentClipboard()
    {
        if (System.Windows.Clipboard.ContainsText())
        {
            string text = System.Windows.Clipboard.GetText();
            return new ClipboardItem
            {
                Kind = ClipboardItemKind.Text,
                Title = "Text",
                Preview = text.Length > 120 ? text[..120] + "..." : text,
                Text = text
            };
        }

        if (System.Windows.Clipboard.ContainsImage())
        {
            BitmapSource image = System.Windows.Clipboard.GetImage();
            return new ClipboardItem
            {
                Kind = ClipboardItemKind.Image,
                Title = "Image",
                Preview = $"Image copied: {image.PixelWidth} x {image.PixelHeight}",
                Image = image
            };
        }

        if (System.Windows.Clipboard.ContainsFileDropList())
        {
            var files = System.Windows.Clipboard.GetFileDropList().Cast<string>().ToArray();
            return new ClipboardItem
            {
                Kind = ClipboardItemKind.Files,
                Title = "Files",
                Preview = string.Join(Environment.NewLine, files.Select(Path.GetFileName)),
                Files = files
            };
        }

        return new ClipboardItem
        {
            Kind = ClipboardItemKind.Unknown,
            Title = "Unknown format",
            Preview = "This clipboard format is not supported by this sample."
        };
    }
}
