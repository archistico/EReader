using EbookReader.Application.Annotations;
using EbookReader.Application.State;
using EbookReader.Cli.Tui;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;
using EbookReader.Layout;

namespace EbookReader.Cli.Tests;

public sealed class ReaderAnnotationSessionTests
{
    [Fact]
    public void OtherBooksConsumeNoteCountBudgetAndDeletingFreesCapacity()
    {
        Book book = CreateBook();
        string path = Path.Combine(Path.GetTempPath(), "ereader-budget-current.epub");
        DateTimeOffset updated = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        ReadingLocation start = new(new SectionId("one"), new BlockId("p"), 0);
        ReadingPersonalNoteSnapshot[] existing = OtherNotes(499, "other")
            .Append(new ReadingPersonalNoteSnapshot(path, book.Id, start, "current", updated)).ToArray();
        ReaderSession session = new(book, new LayoutViewport(12, 4), start,
            initialNotes: ReadingAnnotationState.RestoreNotesForBook(book, path, existing),
            annotationBudget: ReadingAnnotationBudget.ForBook(path, null, existing));

        Assert.True(session.SetNoteAtCurrentLocation("edited", updated));
        Assert.True(session.NextLine());
        Assert.False(session.SetNoteAtCurrentLocation("over budget", updated));
        Assert.Equal("edited", Assert.Single(session.PersonalNotes).Text);
        Assert.True(session.RemoveAnnotation(0));
        Assert.True(session.SetNoteAtCurrentLocation("replacement", updated));

        var merged = ReadingAnnotationState.ReplaceNotesForBook(book, path, existing, session.PersonalNotes);
        Assert.Equal(500, merged.Count);
        Assert.All(existing.Take(499), note => Assert.Contains(note, merged));
        Assert.Equal("replacement", merged.Single(note => note.BookPath == path).Note.Text);
    }

    [Fact]
    public void OtherBooksConsumeNoteTextBudgetAndRejectedEditsPreservePreviousText()
    {
        Book book = CreateBook();
        string path = Path.Combine(Path.GetTempPath(), "ereader-budget-current.epub");
        DateTimeOffset updated = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        ReadingLocation start = new(new SectionId("one"), new BlockId("p"), 0);
        var existing = OtherNotes(63, new string('x', 2_048))
            .Append(new ReadingPersonalNoteSnapshot(
                Path.Combine(Path.GetTempPath(), "ereader-budget-other.epub"), new BookId("other"), start,
                new string('x', 1_024), updated))
            .Append(new ReadingPersonalNoteSnapshot(path, book.Id, start, new string('a', 512), updated)).ToArray();
        ReaderSession session = new(book, new LayoutViewport(12, 4), start,
            initialNotes: ReadingAnnotationState.RestoreNotesForBook(book, path, existing),
            annotationBudget: ReadingAnnotationBudget.ForBook(path, null, existing));

        Assert.False(session.SetNoteAtCurrentLocation(new string('b', 1_025), updated));
        Assert.Equal(new string('a', 512), session.CurrentNote!.Text);
        Assert.True(session.SetNoteAtCurrentLocation(new string('b', 1_024), updated));
        Assert.True(session.NextLine());
        Assert.False(session.SetNoteAtCurrentLocation("x", updated));
        Assert.True(session.NavigateToAnnotation(0));
        Assert.True(session.SetNoteAtCurrentLocation(new string('c', 512), updated));
        Assert.True(session.NextLine());
        Assert.True(session.SetNoteAtCurrentLocation(new string('d', 512), updated));

        var merged = ReadingAnnotationState.ReplaceNotesForBook(book, path, existing, session.PersonalNotes);
        Assert.Equal(ReadingAnnotationState.MaximumTotalNoteTextLength, merged.Sum(note => note.Note.Text.Length));
    }

    [Fact]
    public void OtherBooksConsumeHighlightBudgetAndRemovalRemainsAvailableAtTheLimit()
    {
        Book book = CreateBook();
        string path = Path.Combine(Path.GetTempPath(), "ereader-budget-current.epub");
        ReadingLocation start = new(new SectionId("one"), new BlockId("p"), 0);
        ReadingHighlightRange current = new(start, new ReadingLocation(start.SectionId, start.BlockId, 5));
        var existing = Enumerable.Range(0, 999).Select(index => new ReadingHighlightSnapshot(
            Path.Combine(Path.GetTempPath(), $"ereader-budget-other-{index / 250}.epub"), new BookId("other"),
            new ReadingHighlightRange(
                new ReadingLocation(start.SectionId, start.BlockId, index % 250),
                new ReadingLocation(start.SectionId, start.BlockId, (index % 250) + 1))))
            .Append(new ReadingHighlightSnapshot(path, book.Id, current)).ToArray();
        ReaderSession session = new(book, new LayoutViewport(12, 4), start,
            initialHighlights: ReadingAnnotationState.RestoreHighlightsForBook(book, path, existing),
            annotationBudget: ReadingAnnotationBudget.ForBook(path, existing, null));

        Assert.Equal(HighlightToggleResult.Removed, session.ToggleCurrentLineHighlight());
        Assert.True(session.NextLine());
        Assert.Equal(HighlightToggleResult.Added, session.ToggleCurrentLineHighlight());
        Assert.True(session.NextLine());
        Assert.Equal(HighlightToggleResult.LimitReached, session.ToggleCurrentLineHighlight());

        var merged = ReadingAnnotationState.ReplaceHighlightsForBook(book, path, existing, session.HighlightRanges);
        Assert.Equal(1_000, merged.Count);
        Assert.All(existing.Take(999), highlight => Assert.Contains(highlight, merged));
        Assert.DoesNotContain(current, session.HighlightRanges);
    }

    [Fact]
    public void SamePathAnnotationsOfReplacedPublicationDoNotConsumeBudget()
    {
        Book book = CreateBook();
        string path = Path.Combine(Path.GetTempPath(), "ereader-budget-current.epub");
        string equivalentPath = OperatingSystem.IsWindows() ? path.ToUpperInvariant() : path;
        ReadingLocation start = new(new SectionId("one"), new BlockId("p"), 0);
        ReadingPersonalNoteSnapshot stale = new(equivalentPath, new BookId("old-id"), start,
            new string('x', 2_048), new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));
        ReadingAnnotationBudget budget = ReadingAnnotationBudget.ForBook(path, null, [stale]);

        Assert.Equal(ReadingAnnotationState.MaximumTotalNoteTextLength, budget.MaximumNoteTextLength);
        Assert.Empty(ReadingAnnotationState.RestoreNotesForBook(book, path, [stale]));
        var merged = ReadingAnnotationState.ReplaceNotesForBook(book, path, [stale], []);
        Assert.Empty(merged);
    }

    [Fact]
    public void NotesExceedingTotalTextBudgetAreRejectedAndAcceptedChangesCanBeSaved()
    {
        Book book = new(new BookId("notes"), new BookMetadata("Notes"),
            [new ReadingSection(new SectionId("one"),
                [new ParagraphBlock(new BlockId("p"), [new TextRun(new string('a', 100))])])]);
        DateTimeOffset updated = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        ReadingPersonalNote[] notes = Enumerable.Range(0, 64)
            .Select(index => new ReadingPersonalNote(
                new ReadingLocation(new SectionId("one"), new BlockId("p"), index),
                new string('x', 2_048), updated))
            .ToArray();
        ReaderSession session = new(book, new LayoutViewport(80, 20), initialNotes: notes);

        Assert.False(session.SetNoteAtCurrentLocation("section note", updated));
        Assert.Equal(64, session.PersonalNotes.Count);
        Assert.True(session.ToggleBookmark());
        Assert.True(session.NextLine());

        string directory = Path.Combine(Path.GetTempPath(), $"ereader-annotation-budget-{Guid.NewGuid():N}");
        try
        {
            string bookPath = Path.Combine(directory, "book.epub");
            var merged = ReadingAnnotationState.ReplaceNotesForBook(book, bookPath, null, session.PersonalNotes);
            var bookmarks = ReadingBookmarkState.ReplaceForBook(book, bookPath, null, session.BookmarkLocations);
            ReadingStateSnapshot state = new(
                bookPath, book.Id, session.Location, updated, bookmarks: bookmarks, notes: merged);
            JsonReadingStateStore store = new(Path.Combine(directory, "state.json"));
            store.Save(state);
            ReadingStateSnapshot loaded = store.Load()!;
            Assert.Equal(session.PersonalNotes.Count, loaded.Notes.Count);
            Assert.Equal(session.Location, loaded.Location);
            Assert.Single(loaded.Bookmarks);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

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

    private static IEnumerable<ReadingPersonalNoteSnapshot> OtherNotes(int count, string text) =>
        Enumerable.Range(0, count).Select(index => new ReadingPersonalNoteSnapshot(
            Path.Combine(Path.GetTempPath(), $"ereader-budget-other-notes-{index / 100}.epub"),
            new BookId("other"),
            new ReadingLocation(new SectionId("one"), new BlockId("p"), index % 100),
            text, new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero)));
}
