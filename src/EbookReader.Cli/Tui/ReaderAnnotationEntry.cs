using EbookReader.Application.Annotations;
using EbookReader.Domain.Reading;

namespace EbookReader.Cli.Tui;

public enum ReaderAnnotationKind
{
    Highlight = 0,
    Note = 1,
}

public sealed record ReaderAnnotationEntry(
    ReaderAnnotationKind Kind,
    string Label,
    ReadingLocation Location,
    ReadingHighlightRange? Highlight = null,
    ReadingPersonalNote? Note = null);
