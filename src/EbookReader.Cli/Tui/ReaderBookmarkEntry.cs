using EbookReader.Domain.Reading;

namespace EbookReader.Cli.Tui;

public sealed record ReaderBookmarkEntry(string Label, ReadingLocation Location);
