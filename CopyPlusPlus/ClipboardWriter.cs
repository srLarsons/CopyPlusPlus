using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfMultiCopyClipboard;

internal static class ClipboardWriter
{
    private const string StartFragment = "<!--StartFragment-->";
    private const string EndFragment = "<!--EndFragment-->";

    public static void PutAll(IReadOnlyList<ClipboardItem> items)
    {
        if (items.Count == 0)
            return;

        if (items.Count == 1)
        {
            PutItem(items[0]);
            return;
        }

        var data = new DataObject();
        data.SetText(BuildPlainText(items), TextDataFormat.UnicodeText);

        if (items.Any(i => i.Kind == ClipboardItemKind.Image))
            data.SetData(DataFormats.Html, BuildHtmlClipboard(items));

        Clipboard.SetDataObject(data, true);
    }

    public static void PutItem(ClipboardItem item)
    {
        switch (item.Kind)
        {
            case ClipboardItemKind.Text:
                Clipboard.SetText(item.Text ?? string.Empty, TextDataFormat.UnicodeText);
                break;
            case ClipboardItemKind.Files when item.Files is { Length: > 0 }:
                var files = new StringCollection();
                files.AddRange(item.Files);
                Clipboard.SetFileDropList(files);
                break;
            case ClipboardItemKind.Image when item.Image is BitmapSource image:
                PutImage(image);
                break;
            default:
                throw new InvalidOperationException("This clipboard item cannot be pasted.");
        }
    }

    private static void PutImage(BitmapSource image)
    {
        var data = new DataObject();
        data.SetImage(image);
        data.SetData(DataFormats.Bitmap, image);
        data.SetData(DataFormats.Dib, new MemoryStream(EncodeDib(image)));
        data.SetData("PNG", new MemoryStream(EncodePng(image)));
        Clipboard.SetDataObject(data, true);
    }

    private static string BuildPlainText(IEnumerable<ClipboardItem> items)
    {
        var parts = new List<string>();
        int imageNumber = 1;

        foreach (ClipboardItem item in items)
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
                    parts.Add($"[Image {imageNumber++}]");
                    break;
            }
        }

        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    private static string BuildHtmlClipboard(IEnumerable<ClipboardItem> items)
    {
        string fragment = string.Join(string.Empty, items.Select(ToHtml));
        string html = $"<html><body>{StartFragment}{fragment}{EndFragment}</body></html>";

        const string headerTemplate =
            "Version:0.9\r\nStartHTML:{0:0000000000}\r\nEndHTML:{1:0000000000}\r\nStartFragment:{2:0000000000}\r\nEndFragment:{3:0000000000}\r\n";

        string placeholder = string.Format(headerTemplate, 0, 0, 0, 0);
        int startHtml = Encoding.UTF8.GetByteCount(placeholder);
        int startFragment = startHtml + Encoding.UTF8.GetByteCount(html[..(html.IndexOf(StartFragment, StringComparison.Ordinal) + StartFragment.Length)]);
        int endFragment = startHtml + Encoding.UTF8.GetByteCount(html[..html.IndexOf(EndFragment, StringComparison.Ordinal)]);
        int endHtml = startHtml + Encoding.UTF8.GetByteCount(html);

        return string.Format(headerTemplate, startHtml, endHtml, startFragment, endFragment) + html;
    }

    private static string ToHtml(ClipboardItem item)
    {
        return item.Kind switch
        {
            ClipboardItemKind.Text => $"<p>{EscapeHtml(item.Text ?? string.Empty).Replace(Environment.NewLine, "<br>")}</p>",
            ClipboardItemKind.Files when item.Files is not null => $"<p>{EscapeHtml(string.Join(Environment.NewLine, item.Files)).Replace(Environment.NewLine, "<br>")}</p>",
            ClipboardItemKind.Image when item.Image is not null => $"<p><img src=\"data:image/png;base64,{Convert.ToBase64String(EncodePng(item.Image))}\"></p>",
            _ => string.Empty
        };
    }

    private static byte[] EncodePng(BitmapSource image)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static byte[] EncodeDib(BitmapSource image)
    {
        var encoder = new BmpBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray()[14..];
    }

    private static string EscapeHtml(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
