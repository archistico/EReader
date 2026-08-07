using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Progress;

/// <summary>
/// Precomputed mapping from format-neutral ReadingLocation values to stable logical book progress.
/// The index follows Book.ReadingOrder and measures each block with ContentText.GetPlainText().Length,
/// which is the same UTF-16 coordinate space used by ReadingLocation.CharacterOffset.
/// </summary>
public sealed class BookProgressIndex
{
    private readonly Book _book;
    private readonly Dictionary<SectionId, SectionProgress> _sections = [];

    public BookProgressIndex(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);
        _book = book;

        long total = 0;
        foreach (ReadingSection section in book.ReadingOrder)
        {
            Dictionary<BlockId, long> blocks = [];
            long sectionStart = total;

            foreach (ContentBlock block in section.Blocks)
            {
                int length = ContentText.GetPlainText(block).Length;
                blocks.Add(block.Id, total);
                total = checked(total + length);
            }

            _sections.Add(section.Id, new SectionProgress(sectionStart, blocks));
        }

        TotalUnits = total;
    }

    public long TotalUnits { get; }

    public BookProgress Locate(ReadingLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (!_book.ContainsLocation(location))
        {
            throw new ArgumentOutOfRangeException(nameof(location), location, "La ReadingLocation non appartiene al libro indicizzato.");
        }

        SectionProgress section = _sections[location.SectionId];
        if (location.BlockId is null)
        {
            return new BookProgress(section.StartOffset, TotalUnits);
        }

        long blockStart = section.Blocks[location.BlockId];
        long consumed = checked(blockStart + location.CharacterOffset);
        return new BookProgress(consumed, TotalUnits);
    }

    private sealed record SectionProgress(long StartOffset, Dictionary<BlockId, long> Blocks);
}
