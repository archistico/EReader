using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Reading;

/// <summary>
/// Format-neutral chapter/section navigation expressed exclusively through logical ReadingLocation values.
/// </summary>
public static class LogicalReadingNavigator
{
    public static ReadingLocation ChapterStart(Book book, ReadingLocation current)
    {
        ReadingSection section = RequireSection(book, current);
        return ReadingLocation.AtSectionStart(section.Id);
    }

    public static ReadingLocation ChapterEnd(Book book, ReadingLocation current)
    {
        ReadingSection section = RequireSection(book, current);
        if (section.Blocks.Count == 0)
        {
            return ReadingLocation.AtSectionStart(section.Id);
        }

        ContentBlock lastBlock = section.Blocks[^1];
        return new ReadingLocation(section.Id, lastBlock.Id, ContentText.GetPlainText(lastBlock).Length);
    }

    public static ReadingLocation? NextChapter(Book book, ReadingLocation current) =>
        AdjacentPrimarySection(book, current, +1);

    public static ReadingLocation? PreviousChapter(Book book, ReadingLocation current) =>
        AdjacentPrimarySection(book, current, -1);

    private static ReadingLocation? AdjacentPrimarySection(Book book, ReadingLocation current, int delta)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(current);
        ReadingSection currentSection = RequireSection(book, current);
        int currentIndex = book.ReadingOrder
            .Select((section, index) => (section, index))
            .Single(item => item.section.Id == currentSection.Id)
            .index;

        for (int index = currentIndex + delta; index >= 0 && index < book.ReadingOrder.Count; index += delta)
        {
            ReadingSection section = book.ReadingOrder[index];
            if (section.Role == ReadingSectionRole.Primary)
            {
                return ReadingLocation.AtSectionStart(section.Id);
            }
        }

        return null;
    }

    private static ReadingSection RequireSection(Book book, ReadingLocation current)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(current);
        if (!book.ContainsLocation(current))
        {
            throw new ArgumentOutOfRangeException(nameof(current), "La ReadingLocation non appartiene al libro.");
        }

        return book.FindSection(current.SectionId)
            ?? throw new InvalidOperationException("La ReadingLocation punta a una sezione inesistente.");
    }
}
