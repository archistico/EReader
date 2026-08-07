using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Content;

public sealed class ListItemBlock : ContentBlock
{
    public ListItemBlock(
        BlockId id,
        ListKind listKind,
        IEnumerable<InlineContent> content,
        int depth = 1,
        int? ordinal = null)
        : base(id)
    {
        if (depth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), depth, "La profondità della lista deve essere positiva.");
        }

        if (ordinal is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "L'ordinale, quando presente, deve essere positivo.");
        }

        if (listKind == ListKind.Unordered && ordinal is not null)
        {
            throw new ArgumentException("Una lista non ordinata non può avere un ordinale.", nameof(ordinal));
        }

        ListKind = DomainGuard.DefinedEnum(listKind, nameof(listKind));
        Content = DomainGuard.Freeze(content, nameof(content));
        Depth = depth;
        Ordinal = ordinal;
    }

    public ListKind ListKind { get; }

    public IReadOnlyList<InlineContent> Content { get; }

    public int Depth { get; }

    public int? Ordinal { get; }
}
