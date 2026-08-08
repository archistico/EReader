using System.Collections.ObjectModel;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Links;

/// <summary>
/// Pre-layout index of actionable Domain hyperlinks. Ranges use the same UTF-16 coordinate space
/// as ReadingLocation and therefore do not change when the terminal is resized or rewrapped.
/// </summary>
public sealed class BookHyperlinkIndex
{
    private readonly ReadOnlyCollection<BookHyperlink> _links;

    public BookHyperlinkIndex(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);
        List<BookHyperlink> links = [];

        foreach (ReadingSection section in book.ReadingOrder)
        {
            foreach (ContentBlock block in section.Blocks)
            {
                IReadOnlyList<InlineContent>? content = GetInlineContent(block);
                if (content is null)
                {
                    continue;
                }

                int offset = 0;
                AppendLinks(links, section.Id, block.Id, content, ref offset);
            }
        }

        _links = links.AsReadOnly();
    }

    public ReadOnlyCollection<BookHyperlink> Links => _links;

    public BookHyperlink? FindAt(ReadingLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (location.BlockId is not BlockId blockId)
        {
            return null;
        }

        return _links.FirstOrDefault(link =>
            link.StartLocation.SectionId == location.SectionId
            && link.StartLocation.BlockId == blockId
            && location.CharacterOffset >= link.StartLocation.CharacterOffset
            && location.CharacterOffset < link.EndCharacterOffset);
    }

    public BookHyperlink? FindFirstIntersecting(
        SectionId sectionId,
        BlockId blockId,
        int sourceStartOffset,
        int sourceEndOffset)
    {
        ArgumentNullException.ThrowIfNull(sectionId);
        ArgumentNullException.ThrowIfNull(blockId);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceStartOffset);
        ArgumentOutOfRangeException.ThrowIfLessThan(sourceEndOffset, sourceStartOffset);

        if (sourceStartOffset == sourceEndOffset)
        {
            return null;
        }

        return _links.FirstOrDefault(link =>
            link.StartLocation.SectionId == sectionId
            && link.StartLocation.BlockId == blockId
            && link.StartLocation.CharacterOffset < sourceEndOffset
            && link.EndCharacterOffset > sourceStartOffset);
    }

    private static IReadOnlyList<InlineContent>? GetInlineContent(ContentBlock block) => block switch
    {
        ParagraphBlock paragraph => paragraph.Content,
        HeadingBlock heading => heading.Content,
        QuoteBlock quote => quote.Content,
        ListItemBlock item => item.Content,
        _ => null,
    };

    private static void AppendLinks(
        List<BookHyperlink> links,
        SectionId sectionId,
        BlockId blockId,
        IReadOnlyList<InlineContent> content,
        ref int offset)
    {
        foreach (InlineContent inline in content)
        {
            switch (inline)
            {
                case TextRun run:
                    offset = checked(offset + run.Text.Length);
                    break;

                case LineBreakInline:
                    offset = checked(offset + 1);
                    break;

                case HyperlinkSpan hyperlink:
                    AddHyperlinkAndChildren(links, sectionId, blockId, hyperlink, ref offset);
                    break;

                case InlineContainer container:
                    AppendLinks(links, sectionId, blockId, container.Content, ref offset);
                    break;

                default:
                    throw new NotSupportedException($"Tipo inline non supportato dall'indice hyperlink: {inline.GetType().FullName}.");
            }
        }
    }

    private static void AddHyperlinkAndChildren(
        List<BookHyperlink> links,
        SectionId sectionId,
        BlockId blockId,
        HyperlinkSpan hyperlink,
        ref int offset)
    {
        int start = offset;
        string text = ContentText.GetPlainText(hyperlink.Content);
        if (text.Length > 0)
        {
            links.Add(
                new BookHyperlink(
                    new ReadingLocation(sectionId, blockId, start),
                    text.Length,
                    text,
                    hyperlink.Target,
                    hyperlink.Role));
        }

        AppendLinks(links, sectionId, blockId, hyperlink.Content, ref offset);
        if (offset - start != text.Length)
        {
            throw new InvalidOperationException("Il testo hyperlink e la scansione UTF-16 non coincidono.");
        }
    }
}
