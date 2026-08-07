using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;
using EbookReader.Layout;
using Terminal.Gui.App;

namespace EbookReader.Cli.Tui;

internal static class TerminalGuiReaderHost
{
    public static ReadingLocation Run(Book book, ReadingLocation? initialLocation = null)
    {
        ArgumentNullException.ThrowIfNull(book);

        LayoutViewport viewport = TerminalViewportFactory.CreateForReaderWindow();
        ReaderSession session = new(book, viewport, initialLocation);

        using IApplication app = global::Terminal.Gui.App.Application.Create().Init();
        using ReaderWindow window = new(session);
        app.Run(window);
        return session.Location;
    }
}
