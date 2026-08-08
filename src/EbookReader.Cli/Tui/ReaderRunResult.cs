using System.Collections.ObjectModel;
using EbookReader.Domain.Reading;

namespace EbookReader.Cli.Tui;

internal sealed record ReaderRunResult(
    ReadingLocation Location,
    ReadOnlyCollection<ReadingLocation> Bookmarks,
    string ThemeId);
