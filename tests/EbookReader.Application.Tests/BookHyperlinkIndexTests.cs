using EbookReader.Application.Links;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Tests;

public sealed class BookHyperlinkIndexTests
{
    [Fact]
    public void IndexUsesLogicalUtf16OffsetsAcrossFormatting()
    {
        SectionId sectionId = new("one");
        BlockId blockId = new("p");
        ReadingLocation target = new(sectionId, blockId, 0);
        ParagraphBlock paragraph = new(
            blockId,
            [
                new TextRun("😀 "),
                new HyperlinkSpan(
                    new InternalLinkTarget(target),
                    [new StrongSpan([new TextRun("vai")]), new TextRun(" ora")]),
                new TextRun(" fine"),
            ]);
        Book book = CreateBook(new ReadingSection(sectionId, [paragraph]));

        BookHyperlink link = Assert.Single(new BookHyperlinkIndex(book).Links);

        Assert.Equal(new ReadingLocation(sectionId, blockId, 3), link.StartLocation);
        Assert.Equal(7, link.TextLength);
        Assert.Equal("vai ora", link.Text);
        Assert.IsType<InternalLinkTarget>(link.Target);
    }

    [Fact]
    public void FindAtRequiresLogicalContainment()
    {
        SectionId sectionId = new("one");
        BlockId blockId = new("p");
        ExternalLinkTarget target = new(new Uri("https://example.com/"));
        ParagraphBlock paragraph = new(
            blockId,
            [new TextRun("prima "), new HyperlinkSpan(target, [new TextRun("link")]), new TextRun(" dopo")]);
        BookHyperlinkIndex index = new(CreateBook(new ReadingSection(sectionId, [paragraph])));

        Assert.Null(index.FindAt(new ReadingLocation(sectionId, blockId, 5)));
        Assert.NotNull(index.FindAt(new ReadingLocation(sectionId, blockId, 6)));
        Assert.NotNull(index.FindAt(new ReadingLocation(sectionId, blockId, 9)));
        Assert.Null(index.FindAt(new ReadingLocation(sectionId, blockId, 10)));
    }

    [Fact]
    public void FindFirstIntersectingFindsLinkOnVisualSourceRange()
    {
        SectionId sectionId = new("one");
        BlockId blockId = new("p");
        ParagraphBlock paragraph = new(
            blockId,
            [
                new TextRun("alpha "),
                new HyperlinkSpan(new ExternalLinkTarget(new Uri("https://example.com/")), [new TextRun("beta")]),
                new TextRun(" gamma"),
            ]);
        BookHyperlinkIndex index = new(CreateBook(new ReadingSection(sectionId, [paragraph])));

        BookHyperlink hit = Assert.IsType<BookHyperlink>(index.FindFirstIntersecting(sectionId, blockId, 0, 11));

        Assert.Equal("beta", hit.Text);
        Assert.Null(index.FindFirstIntersecting(sectionId, blockId, 11, 17));
    }

    [Fact]
    public void IndexPreservesNoteReferenceRole()
    {
        SectionId sectionId = new("one");
        BlockId sourceId = new("source");
        BlockId noteId = new("note");
        ParagraphBlock paragraph = new(
            sourceId,
            [new HyperlinkSpan(
                new InternalLinkTarget(new ReadingLocation(sectionId, noteId, 0)),
                [new TextRun("1")],
                HyperlinkRole.NoteReference)]);
        ParagraphBlock note = new(noteId, [new TextRun("Nota")]);
        BookHyperlinkIndex index = new(CreateBook(new ReadingSection(sectionId, [paragraph, note])));

        BookHyperlink link = Assert.Single(index.Links);

        Assert.Equal(HyperlinkRole.NoteReference, link.Role);
    }

    [Fact]
    public void EmptyHyperlinkTextIsNotActionable()
    {
        SectionId sectionId = new("one");
        BlockId blockId = new("p");
        ParagraphBlock paragraph = new(
            blockId,
            [new HyperlinkSpan(new ExternalLinkTarget(new Uri("https://example.com/")), [])]);
        BookHyperlinkIndex index = new(CreateBook(new ReadingSection(sectionId, [paragraph])));

        Assert.Empty(index.Links);
    }

    private static Book CreateBook(params ReadingSection[] sections) =>
        new(new BookId("book-links"), new BookMetadata("Links"), sections);
}
