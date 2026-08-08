using System.Collections.ObjectModel;
using EbookReader.Application.Annotations;
using EbookReader.Domain.Reading;

namespace EbookReader.Cli.Tui;

internal sealed record ReaderRunResult(
    ReadingLocation Location,
    ReadOnlyCollection<ReadingLocation> Bookmarks,
    ReadOnlyCollection<ReadingHighlightRange> Highlights,
    ReadOnlyCollection<ReadingPersonalNote> Notes,
    string ThemeId);
