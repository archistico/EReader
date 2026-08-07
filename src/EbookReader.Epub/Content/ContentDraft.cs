using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Epub.Container;

namespace EbookReader.Epub.Content;

internal sealed class SectionDraft
{
    public SectionDraft(SectionId id, OcfPath sourcePath, bool isLinear)
    {
        Id = id;
        SourcePath = sourcePath;
        IsLinear = isLinear;
    }

    public SectionId Id { get; }

    public OcfPath SourcePath { get; }

    public bool IsLinear { get; }

    public List<BlockDraft> Blocks { get; } = [];

    public string? Title { get; set; }
}

internal sealed class BlockDraft
{
    public BlockDraft(BlockId id, BlockDraftKind kind)
    {
        Id = id;
        Kind = kind;
    }

    public BlockId Id { get; }

    public BlockDraftKind Kind { get; }

    public int LevelOrDepth { get; init; } = 1;

    public ListKind ListKind { get; init; } = ListKind.Unordered;

    public int? Ordinal { get; init; }

    public List<InlineDraft> Inlines { get; } = [];

    public string? Text { get; init; }

    public string? ImageManifestId { get; init; }

    public string? AlternativeText { get; init; }

    public string? Caption { get; init; }
}

internal enum BlockDraftKind
{
    Paragraph,
    Heading,
    Quote,
    ListItem,
    Preformatted,
    Image,
    ThematicBreak,
}

internal abstract record InlineDraft;

internal sealed record TextDraft(string Text) : InlineDraft;

internal sealed record LineBreakDraft : InlineDraft;

internal sealed record EmphasisDraft(List<InlineDraft> Content) : InlineDraft;

internal sealed record StrongDraft(List<InlineDraft> Content) : InlineDraft;

internal sealed record LinkDraft(string Href, OcfPath SourcePath, List<InlineDraft> Content) : InlineDraft;
