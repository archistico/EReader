using EbookReader.Application.Annotations;
using EbookReader.Cli.Tui;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;
using EbookReader.Layout;

namespace EbookReader.Cli.Tests;

public sealed class ReaderAnnotationSessionTests
{
    [Fact]
    public void HighlightToggleCreatesLogicalRangeAndSurvivesReflow()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(10, 4));

        Assert.Equal(HighlightToggleResult.Added, session.ToggleCurrentLineHighlight());
        ReadingHighlightRange highlight = Assert.Single(session.HighlightRanges);
        Assert.True(highlight.End.CharacterOffset > highlight.Start.CharacterOffset);
        Assert.True(session.IsCurrentLineHighlighted);

        Assert.True(session.Reflow(new LayoutViewport(40, 8)));
        Assert.Equal(highlight, Assert.Single(session.HighlightRanges));
        Assert.True(session.IsCurrentLineHighlighted);
    }

    [Fact]
    public void HighlightToggleOnIntersectingLineRemovesExistingRange()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(12, 4));

        Assert.Equal(HighlightToggleResult.Added, session.ToggleCurrentLineHighlight());
        Assert.Equal(HighlightToggleResult.Removed, session.ToggleCurrentLineHighlight());

        Assert.Empty(session.HighlightRanges);
        Assert.False(session.IsCurrentLineHighlighted);
    }

    [Fact]
    public void PersonalNoteCanBeAddedUpdatedAndDeletedAtExactLocation()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(20, 5));
        DateTimeOffset firstTime = new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset secondTime = firstTime.AddMinutes(1);

        Assert.True(session.SetNoteAtCurrentLocation("prima", firstTime));
        Assert.Equal("prima", Assert.Single(session.PersonalNotes).Text);
        Assert.True(session.SetNoteAtCurrentLocation("seconda", secondTime));
        ReadingPersonalNote updated = Assert.Single(session.PersonalNotes);
        Assert.Equal("seconda", updated.Text);
        Assert.Equal(secondTime, updated.UpdatedUtc);

        Assert.False(session.SetNoteAtCurrentLocation(string.Empty, secondTime));
        Assert.Empty(session.PersonalNotes);
        Assert.Null(session.CurrentNote);
    }

    [Fact]
    public void AnnotationListCombinesHighlightAndNoteAndCanNavigateAndDelete()
    {
        Book book = CreateBook();
        ReadingLocation noteLocation = new(new SectionId("one"), new BlockId("p"), 6);
        ReadingPersonalNote note = new(noteLocation, "memo", DateTimeOffset.UtcNow);
        ReaderSession session = new(
            book,
            new LayoutViewport(12, 4),
            initialNotes: [note]);
        Assert.Equal(HighlightToggleResult.Added, session.ToggleCurrentLineHighlight());

        Assert.Equal(2, session.AnnotationCount);
        Assert.Contains(session.AnnotationEntries, item => item.Kind == ReaderAnnotationKind.Highlight);
        Assert.Contains(session.AnnotationEntries, item => item.Kind == ReaderAnnotationKind.Note);
        int noteIndex = session.AnnotationEntries.ToList().FindIndex(item => item.Kind == ReaderAnnotationKind.Note);
        Assert.True(noteIndex >= 0);
        Assert.True(session.NavigateToAnnotation(noteIndex));
        Assert.Equal(noteLocation, session.Location);
        Assert.True(session.RemoveAnnotation(noteIndex));
        Assert.Single(session.AnnotationEntries);
    }

    private static Book CreateBook()
    {
        ReadingSection section = new(
            new SectionId("one"),
            [new ParagraphBlock(
                new BlockId("p"),
                [new TextRun("alpha beta gamma delta epsilon zeta eta theta")])]);
        return new Book(new BookId("annotations"), new BookMetadata("Annotations"), [section]);
    }
}
