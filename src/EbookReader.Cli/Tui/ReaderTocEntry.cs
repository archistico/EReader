using EbookReader.Domain.Reading;

namespace EbookReader.Cli.Tui;

/// <summary>
/// Flattened, UI-neutral projection of one hierarchical table-of-contents node.
/// Depth is preserved for indentation; a null target represents a pure grouping node.
/// </summary>
public sealed record ReaderTocEntry(string Label, int Depth, ReadingLocation? Target)
{
    public bool CanNavigate => Target is not null;
}
