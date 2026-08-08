using EbookReader.Cli.Configuration;
using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;
using EbookReader.Layout;
using Terminal.Gui.App;

namespace EbookReader.Cli.Tui;

internal static class TerminalGuiReaderHost
{
    public static ReaderRunResult Run(
        Book book,
        ReadingLocation? initialLocation = null,
        IEnumerable<ReadingLocation>? initialBookmarks = null,
        ReaderPreferences? preferences = null)
    {
        ArgumentNullException.ThrowIfNull(book);

        LayoutViewport viewport = TerminalViewportFactory.CreateForReaderWindow();
        ReaderSession session = new(book, viewport, initialLocation, initialBookmarks);

        ReaderPreferences effectivePreferences = preferences ?? ReaderPreferences.Default;
        using IApplication app = global::Terminal.Gui.App.Application.Create().Init();
        using ReaderWindow window = new(session, effectivePreferences);
        app.Run(window);
        return new ReaderRunResult(session.Location, session.BookmarkLocations, window.CurrentThemeId);
    }
}
