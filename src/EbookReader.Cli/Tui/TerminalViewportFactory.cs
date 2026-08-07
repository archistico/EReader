using EbookReader.Layout;

namespace EbookReader.Cli.Tui;

internal static class TerminalViewportFactory
{
    private const int DefaultTerminalWidth = 80;
    private const int DefaultTerminalHeight = 24;

    public static LayoutViewport CreateForReaderWindow()
    {
        int terminalWidth = ReadConsoleDimension(() => Console.WindowWidth, DefaultTerminalWidth);
        int terminalHeight = ReadConsoleDimension(() => Console.WindowHeight, DefaultTerminalHeight);

        // Initial bootstrap estimate before Terminal.Gui has completed its first layout pass.
        int contentWidth = Math.Max(2, terminalWidth - 2);
        int contentHeight = Math.Max(1, terminalHeight - 4);
        return new LayoutViewport(contentWidth, contentHeight);
    }

    public static LayoutViewport CreateFromBodyViewport(int width, int height) =>
        new(Math.Max(2, width), Math.Max(1, height));

    private static int ReadConsoleDimension(Func<int> getter, int fallback)
    {
        try
        {
            int value = getter();
            return value > 0 ? value : fallback;
        }
        catch (IOException)
        {
            return fallback;
        }
        catch (PlatformNotSupportedException)
        {
            return fallback;
        }
    }
}
