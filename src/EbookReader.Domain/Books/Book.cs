using EbookReader.Domain.Content;
using EbookReader.Domain.Internal;
using EbookReader.Domain.Navigation;
using EbookReader.Domain.Reading;
using EbookReader.Domain.Resources;

namespace EbookReader.Domain.Books;

public sealed class Book
{
    public Book(
        BookId id,
        BookMetadata metadata,
        IEnumerable<ReadingSection> readingOrder,
        TableOfContents? tableOfContents = null,
        IEnumerable<BookResource>? resources = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(metadata);

        Id = id;
        Metadata = metadata;
        ReadingOrder = DomainGuard.Freeze(readingOrder, nameof(readingOrder));
        TableOfContents = tableOfContents ?? TableOfContents.Empty;
        Resources = DomainGuard.Freeze(resources ?? Array.Empty<BookResource>(), nameof(resources));

        ValidateAggregate();
    }

    public BookId Id { get; }

    public BookMetadata Metadata { get; }

    public IReadOnlyList<ReadingSection> ReadingOrder { get; }

    public TableOfContents TableOfContents { get; }

    public IReadOnlyList<BookResource> Resources { get; }

    public ReadingSection? FindSection(SectionId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return ReadingOrder.FirstOrDefault(section => section.Id == id);
    }

    public bool ContainsLocation(ReadingLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        ReadingSection? section = FindSection(location.SectionId);
        if (section is null)
        {
            return false;
        }

        if (location.BlockId is null)
        {
            return location.CharacterOffset == 0;
        }

        ContentBlock? block = section.FindBlock(location.BlockId);
        if (block is null)
        {
            return false;
        }

        int textLength = ContentText.GetPlainText(block).Length;
        return location.CharacterOffset <= textLength;
    }

    private void ValidateAggregate()
    {
        if (ReadingOrder.Count == 0)
        {
            throw new InvalidOperationException("Il reading order deve contenere almeno una sezione.");
        }

        EnsureUniqueSectionIds();
        EnsurePrimaryReadingSectionExists();
        EnsureUniqueResourceIds();
        ValidateContentReferences();
        ValidateNavigation(TableOfContents.Items);
    }

    private void EnsureUniqueSectionIds()
    {
        SectionId[] duplicates = ReadingOrder
            .GroupBy(section => section.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"Il reading order contiene SectionId duplicati: {string.Join(", ", duplicates)}.");
        }
    }

    private void EnsurePrimaryReadingSectionExists()
    {
        if (!ReadingOrder.Any(section => section.Role == ReadingSectionRole.Primary))
        {
            throw new InvalidOperationException("Il libro deve contenere almeno una sezione primaria.");
        }
    }

    private void EnsureUniqueResourceIds()
    {
        ResourceId[] duplicates = Resources
            .GroupBy(resource => resource.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"Il libro contiene ResourceId duplicati: {string.Join(", ", duplicates)}.");
        }
    }

    private void ValidateContentReferences()
    {
        foreach (ReadingSection section in ReadingOrder)
        {
            foreach (ContentBlock block in section.Blocks)
            {
                if (block is ImageBlock image)
                {
                    BookResource? resource = Resources.FirstOrDefault(candidate => candidate.Id == image.ResourceId);
                    if (resource is null)
                    {
                        throw new InvalidOperationException(
                            $"Il blocco immagine '{block.Id}' riferisce la risorsa mancante '{image.ResourceId}'.");
                    }

                    if (resource.Kind != ResourceKind.Image)
                    {
                        throw new InvalidOperationException(
                            $"Il blocco immagine '{block.Id}' riferisce una risorsa non-image '{image.ResourceId}'.");
                    }
                }

                ValidateInlineLinks(section, block);
            }
        }
    }

    private void ValidateInlineLinks(ReadingSection section, ContentBlock block)
    {
        IEnumerable<InlineContent> content = block switch
        {
            ParagraphBlock paragraph => paragraph.Content,
            HeadingBlock heading => heading.Content,
            QuoteBlock quote => quote.Content,
            ListItemBlock item => item.Content,
            _ => Array.Empty<InlineContent>(),
        };

        foreach (InlineContent inline in EnumerateInline(content))
        {
            if (inline is HyperlinkSpan { Target: InternalLinkTarget internalTarget }
                && !ContainsLocation(internalTarget.Location))
            {
                throw new InvalidOperationException(
                    $"Il blocco '{block.Id}' della sezione '{section.Id}' contiene un link interno non risolvibile.");
            }
        }
    }

    private void ValidateNavigation(IEnumerable<NavigationItem> items)
    {
        foreach (NavigationItem item in items)
        {
            if (item.Target is not null && !ContainsLocation(item.Target))
            {
                throw new InvalidOperationException(
                    $"La voce TOC '{item.Label}' punta a una ReadingLocation non risolvibile.");
            }

            ValidateNavigation(item.Children);
        }
    }

    private static IEnumerable<InlineContent> EnumerateInline(IEnumerable<InlineContent> content)
    {
        foreach (InlineContent inline in content)
        {
            yield return inline;

            if (inline is InlineContainer container)
            {
                foreach (InlineContent child in EnumerateInline(container.Content))
                {
                    yield return child;
                }
            }
        }
    }
}
