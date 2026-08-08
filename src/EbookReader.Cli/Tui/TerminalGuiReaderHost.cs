using EbookReader.Application.Annotations;
using EbookReader.Cli.Configuration;
using EbookReader.Cli.Images;
using EbookReader.Cli.Links;
using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;
using EbookReader.Layout;
using Terminal.Gui.App;

namespace EbookReader.Cli.Tui;

internal static class TerminalGuiReaderHost
{
    public static ReaderRunResult Run(
        Book book,
        string epubFilePath,
        ReadingLocation? initialLocation = null,
        IEnumerable<ReadingLocation>? initialBookmarks = null,
        IEnumerable<ReadingHighlightRange>? initialHighlights = null,
        IEnumerable<ReadingPersonalNote>? initialNotes = null,
        ReaderPreferences? preferences = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(epubFilePath);

        LayoutViewport viewport = TerminalViewportFactory.CreateForReaderWindow();
        ReaderSession session = new(
            book,
            viewport,
            initialLocation,
            initialBookmarks,
            initialHighlights,
            initialNotes);

        ReaderPreferences effectivePreferences = preferences ?? ReaderPreferences.Default;
        using ExternalImagePreviewService imagePreviewService = new(epubFilePath);
        SystemExternalLinkService externalLinkService = new();
        using IApplication app = global::Terminal.Gui.App.Application.Create().Init();
        using ReaderWindow window = new(session, effectivePreferences, imagePreviewService, externalLinkService);
        app.Run(window);
        return new ReaderRunResult(
            session.Location,
            session.BookmarkLocations,
            session.HighlightRanges,
            session.PersonalNotes,
            window.CurrentThemeId);
    }
}
