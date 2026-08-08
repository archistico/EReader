using EbookReader.Application.Links;
using EbookReader.Application.Progress;
using EbookReader.Cli.Tui;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Navigation;
using EbookReader.Domain.Reading;
using EbookReader.Domain.Resources;
using EbookReader.Layout;

namespace EbookReader.Cli.Tests;

public sealed class ReaderSessionTests
{
    [Fact]
    public void CurrentImageProjectsDomainImageAndResourceMetadata()
    {
        ResourceId resourceId = new("figure");
        ImageBlock image = new(new BlockId("img"), resourceId, "Schema", "Figura uno");
        ReadingSection section = new(new SectionId("one"), [image, new ParagraphBlock(new BlockId("p"), [new TextRun("testo")])]);
        BookResource resource = new(resourceId, ResourceKind.Image, "image/png", "images/figure.png");
        Book book = new(new BookId("image-book"), new BookMetadata("Immagini"), [section], resources: [resource]);
        ReaderSession atSectionStart = new(book, new LayoutViewport(40, 10));
        Assert.NotNull(atSectionStart.CurrentImage);

        ReaderSession session = new(
            book,
            new LayoutViewport(40, 10),
            new ReadingLocation(section.Id, image.Id, 0));

        ReaderImageInfo? current = session.CurrentImage;

        Assert.NotNull(current);
        Assert.Equal(resourceId, current.ResourceId);
        Assert.Equal("image/png", current.MediaType);
        Assert.Equal("Schema", current.AlternativeText);
        Assert.Equal("Figura uno", current.Caption);
        Assert.Equal("images/figure.png", current.ResourceName);
        Assert.True(session.NextLine());
        Assert.Null(session.CurrentImage);
    }

    [Fact]
    public void StartsAtFirstPrimarySectionAndFirstPage()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(12, 3));

        Assert.Equal(new SectionId("one"), session.Location.SectionId);
        Assert.Equal(1, session.PageNumber);
        Assert.True(session.PageCount > 1);
    }

    [Fact]
    public void NextAndPreviousPageKeepLogicalReadingLocation()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(12, 3));
        ReadingLocation initial = session.Location;

        Assert.True(session.NextPage());
        Assert.NotEqual(initial, session.Location);
        Assert.Equal(2, session.PageNumber);
        Assert.True(session.PreviousPage());
        Assert.Equal(1, session.PageNumber);
    }

    [Fact]
    public void LineNavigationUsesMappedLinesRatherThanSpacing()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(40, 10));

        Assert.True(session.NextLine());
        Assert.NotNull(session.Location.BlockId);
        Assert.Equal(0, session.Location.CharacterOffset);
    }

    [Fact]
    public void ChapterNavigationUsesPrimarySections()
    {
        Book book = CreateBook(includeSupplementary: true);
        ReaderSession session = new(book, new LayoutViewport(20, 5));

        Assert.True(session.NextChapter());
        Assert.Equal(new SectionId("two"), session.Location.SectionId);
        Assert.Equal(2, session.CurrentPrimarySectionNumber);
        Assert.True(session.PreviousChapter());
        Assert.Equal(new SectionId("one"), session.Location.SectionId);
    }

    [Fact]
    public void ChapterStartAndEndRemainLogicalLocations()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(12, 3));

        Assert.True(session.NextLine());
        Assert.True(session.ChapterEnd());
        Assert.Equal(new BlockId("p2"), session.Location.BlockId);
        Assert.True(session.Location.CharacterOffset > 0);
        Assert.True(session.ChapterStart());
        Assert.Null(session.Location.BlockId);
        Assert.Equal(0, session.Location.CharacterOffset);
    }

    [Fact]
    public void HeaderMetadataExposesTitleAuthorsAndChapterCounts()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(20, 5));

        Assert.Equal("Libro TUI", session.BookTitle);
        Assert.Equal("Emilie Rollandin", session.AuthorLine);
        Assert.Equal(1, session.CurrentPrimarySectionNumber);
        Assert.Equal(2, session.PrimarySectionCount);
    }

    [Fact]
    public void RenderCurrentPageUsesDeterministicLayoutLines()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(12, 3));

        string page = session.RenderCurrentPage();

        Assert.Contains("Capitolo Uno", page, StringComparison.Ordinal);
    }

    [Fact]
    public void LineNavigationChangesSlidingViewportBeforePageBoundary()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(12, 3));

        int pageBefore = session.PageNumber;
        string before = session.RenderCurrentViewport();

        Assert.True(session.NextLine());

        Assert.Equal(pageBefore, session.PageNumber);
        Assert.NotEqual(before, session.RenderCurrentViewport());
    }

    [Fact]
    public void PreviousLineRestoresSlidingViewport()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(12, 3));
        string initial = session.RenderCurrentViewport();

        Assert.True(session.NextLine());
        Assert.True(session.PreviousLine());

        Assert.Equal(initial, session.RenderCurrentViewport());
    }

    [Fact]
    public void NavigationAtBookBoundariesReturnsFalse()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(40, 20));

        Assert.False(session.PreviousLine());
        Assert.False(session.PreviousPage());
        Assert.False(session.PreviousChapter());

        Assert.True(session.NextChapter());
        Assert.False(session.NextChapter());
    }

    [Fact]
    public void EmptyReadableProjectionStillProducesAnEmptyFirstPage()
    {
        ReadingSection section = new(new SectionId("empty"), [new ParagraphBlock(new BlockId("p"))]);
        Book book = new(new BookId("book-empty"), new BookMetadata("Vuoto"), [section]);
        ReaderSession session = new(book, new LayoutViewport(20, 5));

        Assert.False(session.HasReadableContent);
        Assert.Equal(1, session.PageNumber);
        Assert.Equal(1, session.PageCount);
        Assert.Equal(string.Empty, session.RenderCurrentPage());
        Assert.False(session.NextPage());
    }


    [Fact]
    public void ReflowPreservesExactLogicalLocationAcrossViewportChanges()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(12, 3));

        Assert.True(session.NextPage());
        ReadingLocation before = session.Location;
        LayoutPosition? positionBefore = session.Position;

        Assert.True(session.Reflow(new LayoutViewport(40, 10)));

        Assert.Equal(before, session.Location);
        Assert.Equal(new LayoutViewport(40, 10), session.Layout.Viewport);
        Assert.NotNull(session.Position);
        Assert.NotNull(positionBefore);
    }

    [Fact]
    public void ReflowWithSameViewportIsNoOp()
    {
        Book book = CreateBook();
        LayoutViewport viewport = new(20, 5);
        ReaderSession session = new(book, viewport);
        BookLayout before = session.Layout;
        ReadingLocation location = session.Location;

        Assert.False(session.Reflow(new LayoutViewport(20, 5)));

        Assert.Same(before, session.Layout);
        Assert.Equal(location, session.Location);
    }

    [Fact]
    public void ReflowPreservesChapterEndLocation()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(12, 3));

        Assert.True(session.ChapterEnd());
        ReadingLocation chapterEnd = session.Location;

        Assert.True(session.Reflow(new LayoutViewport(80, 24)));

        Assert.Equal(chapterEnd, session.Location);
        Assert.NotNull(session.Position);
    }

    [Fact]
    public void ReflowKeepsEmptyProjectionStable()
    {
        ReadingSection section = new(new SectionId("empty"), [new ParagraphBlock(new BlockId("p"))]);
        Book book = new(new BookId("book-empty"), new BookMetadata("Vuoto"), [section]);
        ReaderSession session = new(book, new LayoutViewport(20, 5));
        ReadingLocation location = session.Location;

        Assert.True(session.Reflow(new LayoutViewport(40, 10)));

        Assert.Equal(location, session.Location);
        Assert.False(session.HasReadableContent);
        Assert.Equal(1, session.PageNumber);
        Assert.Equal(string.Empty, session.RenderCurrentPage());
    }


    [Fact]
    public void StartsAtProvidedPersistedLogicalLocation()
    {
        Book book = CreateBook();
        ReadingLocation restored = new(new SectionId("one"), new BlockId("p1"), 6);

        ReaderSession session = new(book, new LayoutViewport(12, 3), restored);

        Assert.Equal(restored, session.Location);
        Assert.NotNull(session.Position);
    }

    [Fact]
    public void RejectsPersistedLocationOutsideBook()
    {
        Book book = CreateBook();
        ReadingLocation foreign = ReadingLocation.AtSectionStart(new SectionId("missing"));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReaderSession(book, new LayoutViewport(12, 3), foreign));
    }


    [Fact]
    public void FlattensHierarchicalTableOfContentsAndPreservesDepth()
    {
        Book book = CreateBookWithToc();
        ReaderSession session = new(book, new LayoutViewport(20, 5));

        Assert.Equal(4, session.TocEntries.Count);
        Assert.Equal("Parte I", session.TocEntries[0].Label);
        Assert.Equal(0, session.TocEntries[0].Depth);
        Assert.False(session.TocEntries[0].CanNavigate);
        Assert.Equal("Capitolo Uno", session.TocEntries[1].Label);
        Assert.Equal(1, session.TocEntries[1].Depth);
        Assert.True(session.TocEntries[1].CanNavigate);
        Assert.Equal("Sezione gamma", session.TocEntries[2].Label);
        Assert.Equal(1, session.TocEntries[2].Depth);
        Assert.Equal("Capitolo Due", session.TocEntries[3].Label);
        Assert.Equal(0, session.TocEntries[3].Depth);
    }

    [Fact]
    public void SuggestedTocEntryTracksNearestPrecedingLogicalTarget()
    {
        Book book = CreateBookWithToc();
        ReadingLocation insideFirstParagraph = new(new SectionId("one"), new BlockId("p1"), 20);
        ReaderSession session = new(book, new LayoutViewport(20, 5), insideFirstParagraph);

        Assert.Equal(2, session.SuggestedTocEntryIndex);
        Assert.Equal("Sezione gamma", session.TocEntries[session.SuggestedTocEntryIndex].Label);
    }

    [Fact]
    public void TocNavigationMovesToDomainReadingLocation()
    {
        Book book = CreateBookWithToc();
        ReaderSession session = new(book, new LayoutViewport(20, 5));

        Assert.True(session.NavigateToTocEntry(3));
        Assert.Equal(ReadingLocation.AtSectionStart(new SectionId("two")), session.Location);
        Assert.Equal(2, session.CurrentPrimarySectionNumber);
    }

    [Fact]
    public void TocGroupingNodeIsNotANavigationDestination()
    {
        Book book = CreateBookWithToc();
        ReaderSession session = new(book, new LayoutViewport(20, 5));
        ReadingLocation before = session.Location;

        Assert.False(session.NavigateToTocEntry(0));
        Assert.Equal(before, session.Location);
        Assert.Equal(1, session.FindAdjacentNavigableTocEntry(-1, 1));
        Assert.Equal(2, session.FindAdjacentNavigableTocEntry(1, 1));
        Assert.Equal(1, session.FindAdjacentNavigableTocEntry(1, -1));
    }

    [Fact]
    public void MetadataProjectionUsesOnlyFormatNeutralBookMetadata()
    {
        Book book = CreateBookWithMetadata();
        ReaderSession session = new(book, new LayoutViewport(40, 10));

        Assert.Contains(session.MetadataEntries, entry => entry == new ReaderMetadataEntry("Titolo", "Metadati completi"));
        Assert.Contains(session.MetadataEntries, entry => entry == new ReaderMetadataEntry("Sottotitolo", "Una prova"));
        Assert.Contains(session.MetadataEntries, entry => entry == new ReaderMetadataEntry("Autore", "Emilie Rollandin (ordinamento: Rollandin, Emilie)"));
        Assert.Contains(session.MetadataEntries, entry => entry == new ReaderMetadataEntry("Traduttore", "Mario Rossi"));
        Assert.Contains(session.MetadataEntries, entry => entry == new ReaderMetadataEntry("Lingue", "it, en"));
        Assert.Contains(session.MetadataEntries, entry => entry == new ReaderMetadataEntry("Editore", "EReader Press"));
        Assert.Contains(session.MetadataEntries, entry => entry == new ReaderMetadataEntry("Identificatore (ISBN)", "9780000000001"));
        Assert.Contains(session.MetadataEntries, entry => entry == new ReaderMetadataEntry("Argomenti", "CLI, EPUB"));
        Assert.Contains(session.MetadataEntries, entry => entry == new ReaderMetadataEntry("Diritti", "CC BY"));
        Assert.Contains(session.MetadataEntries, entry => entry == new ReaderMetadataEntry("Descrizione", "Descrizione lunga del libro."));
        Assert.Contains(session.MetadataEntries, entry => entry == new ReaderMetadataEntry("Book ID", "book-metadata"));
    }

    [Fact]
    public void MetadataProjectionOmitsMissingOptionalFields()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(40, 10));

        Assert.DoesNotContain(session.MetadataEntries, entry => entry.Label == "Sottotitolo");
        Assert.DoesNotContain(session.MetadataEntries, entry => entry.Label == "Editore");
        Assert.DoesNotContain(session.MetadataEntries, entry => entry.Label == "Descrizione");
        Assert.Contains(session.MetadataEntries, entry => entry.Label == "Titolo");
        Assert.Contains(session.MetadataEntries, entry => entry.Label == "Book ID");
    }

    [Fact]
    public void SearchSelectsFirstLogicalMatchAtOrAfterCurrentLocation()
    {
        ReadingSection section = new(
            new SectionId("one"),
            [new ParagraphBlock(new BlockId("p"), [new TextRun("alpha xx alpha")])]);
        Book book = new(new BookId("search-book"), new BookMetadata("Search"), [section]);
        ReadingLocation current = new(section.Id, new BlockId("p"), 4);
        ReaderSession session = new(book, new LayoutViewport(12, 3), current);

        Assert.True(session.Search("alpha"));

        Assert.Equal("alpha", session.SearchQuery);
        Assert.Equal(2, session.SearchMatchCount);
        Assert.Equal(new ReadingLocation(section.Id, new BlockId("p"), 9), session.Location);
        Assert.Equal(2, session.CurrentSearchMatchNumber);
    }

    [Fact]
    public void SearchNextAndPreviousResultsWrapAround()
    {
        ReadingSection section = new(
            new SectionId("one"),
            [new ParagraphBlock(new BlockId("p"), [new TextRun("alpha xx alpha yy alpha")])]);
        Book book = new(new BookId("search-book"), new BookMetadata("Search"), [section]);
        ReaderSession session = new(book, new LayoutViewport(20, 5));

        Assert.True(session.Search("alpha"));
        Assert.Equal(1, session.CurrentSearchMatchNumber);
        Assert.Equal(0, session.Location.CharacterOffset);

        Assert.True(session.NextSearchResult());
        Assert.Equal(2, session.CurrentSearchMatchNumber);
        Assert.Equal(9, session.Location.CharacterOffset);

        Assert.True(session.PreviousSearchResult());
        Assert.Equal(1, session.CurrentSearchMatchNumber);
        Assert.Equal(0, session.Location.CharacterOffset);

        Assert.True(session.PreviousSearchResult());
        Assert.Equal(3, session.CurrentSearchMatchNumber);
        Assert.Equal(18, session.Location.CharacterOffset);
    }

    [Fact]
    public void SearchWithNoMatchesKeepsReadingLocationAndExposesZeroResults()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(20, 5));
        ReadingLocation before = session.Location;

        Assert.False(session.Search("parola-che-non-esiste"));

        Assert.Equal(before, session.Location);
        Assert.Equal("parola-che-non-esiste", session.SearchQuery);
        Assert.Equal(0, session.SearchMatchCount);
        Assert.Equal(0, session.CurrentSearchMatchNumber);
        Assert.False(session.NextSearchResult());
        Assert.False(session.PreviousSearchResult());
    }

    [Fact]
    public void SearchLocationSurvivesReflowBecauseResultsArePreLayout()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(12, 3));

        Assert.True(session.Search("epsilon"));
        ReadingLocation match = session.Location;

        Assert.True(session.Reflow(new LayoutViewport(80, 24)));

        Assert.Equal(match, session.Location);
        Assert.Equal("epsilon", session.SearchQuery);
        Assert.Equal(1, session.CurrentSearchMatchNumber);
    }

    [Fact]
    public void BookmarkToggleAddsAndRemovesExactLogicalLocation()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(20, 5));
        ReadingLocation location = session.Location;

        Assert.True(session.ToggleBookmark());
        Assert.True(session.IsCurrentLocationBookmarked);
        Assert.Equal(1, session.BookmarkCount);
        Assert.Equal(location, Assert.Single(session.BookmarkLocations));

        Assert.False(session.ToggleBookmark());
        Assert.False(session.IsCurrentLocationBookmarked);
        Assert.Empty(session.BookmarkLocations);
    }

    [Fact]
    public void InitialBookmarksAreSortedByReadingOrderAndCanBeNavigated()
    {
        Book book = CreateBook();
        ReadingLocation later = new(new SectionId("two"), new BlockId("p3"), 2);
        ReadingLocation earlier = new(new SectionId("one"), new BlockId("p1"), 6);
        ReaderSession session = new(
            book,
            new LayoutViewport(20, 5),
            initialBookmarks: [later, earlier]);

        Assert.Equal(2, session.BookmarkCount);
        Assert.Equal(earlier, session.BookmarkLocations[0]);
        Assert.Equal(later, session.BookmarkLocations[1]);

        Assert.True(session.NavigateToBookmark(1));
        Assert.Equal(later, session.Location);
    }

    [Fact]
    public void BookmarkLocationsSurviveReflowAndDoNotDependOnPages()
    {
        Book book = CreateBook();
        ReadingLocation bookmark = new(new SectionId("one"), new BlockId("p1"), 10);
        ReaderSession session = new(
            book,
            new LayoutViewport(12, 3),
            initialBookmarks: [bookmark]);

        Assert.True(session.Reflow(new LayoutViewport(80, 24)));

        ReadingLocation only = Assert.Single(session.BookmarkLocations);
        Assert.Equal(bookmark, only);
        Assert.Contains(session.BookmarkEntries, entry => entry.Location == bookmark);
    }

    [Fact]
    public void RemovingBookmarkFromListDoesNotMoveReadingLocation()
    {
        Book book = CreateBook();
        ReadingLocation first = new(new SectionId("one"), new BlockId("p1"), 5);
        ReadingLocation second = new(new SectionId("two"), new BlockId("p3"), 1);
        ReaderSession session = new(
            book,
            new LayoutViewport(20, 5),
            initialBookmarks: [first, second]);
        ReadingLocation before = session.Location;

        Assert.True(session.RemoveBookmark(0));

        Assert.Equal(before, session.Location);
        ReadingLocation only = Assert.Single(session.BookmarkLocations);
        Assert.Equal(second, only);
    }

    [Fact]
    public void StableProgressAdvancesWithLogicalReadingLocation()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(20, 5));

        Assert.Equal(0m, session.Progress.Percentage);
        Assert.True(session.Search("epsilon"));
        Assert.True(session.Progress.ConsumedUnits > 0);
        Assert.True(session.Progress.Percentage > 0m);
        Assert.True(session.Progress.Percentage < 100m);
    }

    [Fact]
    public void StableProgressDoesNotChangeAcrossReflow()
    {
        Book book = CreateBook();
        ReaderSession session = new(book, new LayoutViewport(12, 3));

        Assert.True(session.Search("epsilon"));
        BookProgress before = session.Progress;
        Assert.True(session.Reflow(new LayoutViewport(80, 24)));

        Assert.Equal(before, session.Progress);
        Assert.Equal(before.Percentage, session.Progress.Percentage);
        Assert.Equal(new LayoutViewport(80, 24), session.Layout.Viewport);
    }

    [Fact]
    public void CurrentHyperlinkFindsFirstLinkOnCurrentVisualLine()
    {
        Book book = CreateBookWithInternalLink();
        ReaderSession session = new(book, new LayoutViewport(40, 10));

        BookHyperlink link = Assert.IsType<BookHyperlink>(session.CurrentHyperlink);

        Assert.Equal("secondo", link.Text);
        Assert.IsType<InternalLinkTarget>(link.Target);
    }

    [Fact]
    public void InternalHyperlinkUsesTransientBackStack()
    {
        Book book = CreateBookWithInternalLink();
        ReaderSession session = new(book, new LayoutViewport(40, 10));
        ReadingLocation origin = session.Location;

        Assert.False(session.CanNavigateBack);
        Assert.True(session.FollowCurrentInternalHyperlink());
        Assert.Equal(new ReadingLocation(new SectionId("one"), new BlockId("target"), 0), session.Location);
        Assert.True(session.CanNavigateBack);
        Assert.True(session.NavigateBack());
        Assert.Equal(origin, session.Location);
        Assert.False(session.CanNavigateBack);
    }

    [Fact]
    public void CurrentHyperlinkRemainsLogicalAcrossReflow()
    {
        Book book = CreateBookWithInternalLink();
        ReaderSession session = new(book, new LayoutViewport(20, 5));
        BookHyperlink before = Assert.IsType<BookHyperlink>(session.CurrentHyperlink);

        Assert.True(session.Reflow(new LayoutViewport(80, 24)));

        BookHyperlink after = Assert.IsType<BookHyperlink>(session.CurrentHyperlink);
        Assert.Equal(before.StartLocation, after.StartLocation);
        Assert.Equal(before.Text, after.Text);
    }

    [Fact]
    public void ExternalHyperlinkDoesNotEnterInternalBackStack()
    {
        SectionId sectionId = new("one");
        BlockId blockId = new("external");
        ParagraphBlock paragraph = new(
            blockId,
            [new HyperlinkSpan(new ExternalLinkTarget(new Uri("https://example.com/")), [new TextRun("Example")])]);
        Book book = new(new BookId("external-book"), new BookMetadata("External"), [new ReadingSection(sectionId, [paragraph])]);
        ReaderSession session = new(book, new LayoutViewport(40, 10));

        Assert.IsType<ExternalLinkTarget>(session.CurrentHyperlink?.Target);
        Assert.False(session.FollowCurrentInternalHyperlink());
        Assert.False(session.CanNavigateBack);
    }

    private static Book CreateBookWithInternalLink()
    {
        SectionId sectionId = new("one");
        BlockId sourceId = new("source");
        BlockId targetId = new("target");
        ReadingLocation target = new(sectionId, targetId, 0);
        ParagraphBlock source = new(
            sourceId,
            [new TextRun("Vai al "), new HyperlinkSpan(new InternalLinkTarget(target), [new TextRun("secondo")]), new TextRun(" paragrafo")]);
        ParagraphBlock destination = new(targetId, [new TextRun("Destinazione")]);
        ReadingSection section = new(sectionId, [source, destination]);
        return new Book(new BookId("link-book"), new BookMetadata("Link Book"), [section]);
    }

    private static Book CreateBook(bool includeSupplementary = false)
    {
        ReadingSection one = new(
            new SectionId("one"),
            [
                new HeadingBlock(new BlockId("h1"), 1, [new TextRun("Capitolo Uno")]),
                new ParagraphBlock(new BlockId("p1"), [new TextRun("alpha beta gamma delta epsilon zeta eta theta")]),
                new ParagraphBlock(new BlockId("p2"), [new TextRun("fine primo capitolo")]),
            ],
            title: "Capitolo Uno");

        ReadingSection supplementary = new(
            new SectionId("notes"),
            [new ParagraphBlock(new BlockId("note"), [new TextRun("nota")])],
            title: "Note",
            role: ReadingSectionRole.Supplementary);

        ReadingSection two = new(
            new SectionId("two"),
            [new ParagraphBlock(new BlockId("p3"), [new TextRun("secondo capitolo")])],
            title: "Capitolo Due");

        List<ReadingSection> sections = [one];
        if (includeSupplementary)
        {
            sections.Add(supplementary);
        }
        sections.Add(two);

        BookMetadata metadata = new(
            "Libro TUI",
            contributors: [new BookContributor("Emilie Rollandin", ContributorRole.Author)]);
        return new Book(new BookId("book"), metadata, sections);
    }
    private static Book CreateBookWithMetadata()
    {
        ReadingSection section = new(
            new SectionId("one"),
            [new ParagraphBlock(new BlockId("p"), [new TextRun("contenuto")])],
            title: "Capitolo");
        BookMetadata metadata = new(
            "Metadati completi",
            subtitle: "Una prova",
            languages: ["it", "en"],
            contributors:
            [
                new BookContributor("Emilie Rollandin", ContributorRole.Author, "Rollandin, Emilie"),
                new BookContributor("Mario Rossi", ContributorRole.Translator),
            ],
            identifiers: [new BookIdentifier("9780000000001", "ISBN")],
            description: "Descrizione lunga del libro.",
            publisher: "EReader Press",
            subjects: ["CLI", "EPUB"],
            rights: "CC BY");
        return new Book(new BookId("book-metadata"), metadata, [section]);
    }

    private static Book CreateBookWithToc()
    {
        Book baseBook = CreateBook();
        ReadingLocation chapterOne = ReadingLocation.AtSectionStart(new SectionId("one"));
        ReadingLocation gamma = new(new SectionId("one"), new BlockId("p1"), 11);
        ReadingLocation chapterTwo = ReadingLocation.AtSectionStart(new SectionId("two"));

        TableOfContents toc = new(
        [
            new NavigationItem(
                "Parte I",
                null,
                [
                    new NavigationItem("Capitolo Uno", chapterOne),
                    new NavigationItem("Sezione gamma", gamma),
                ]),
            new NavigationItem("Capitolo Due", chapterTwo),
        ]);

        return new Book(baseBook.Id, baseBook.Metadata, baseBook.ReadingOrder, toc, baseBook.Resources);
    }

}
