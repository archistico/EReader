namespace EbookReader.Domain.Tests.Books;

public sealed class BookTests
{
    [Fact]
    public void BookRequiresAtLeastOneSection()
    {
        Assert.Throws<InvalidOperationException>(
            () => new Book(new BookId("book"), new BookMetadata("Title"), []));
    }

    [Fact]
    public void BookRequiresPrimarySection()
    {
        ReadingSection notes = new(
            new SectionId("notes"),
            [],
            role: ReadingSectionRole.Supplementary);

        Assert.Throws<InvalidOperationException>(
            () => new Book(new BookId("book"), new BookMetadata("Title"), [notes]));
    }

    [Fact]
    public void BookRejectsDuplicateSectionIds()
    {
        ReadingSection first = new(new SectionId("same"), []);
        ReadingSection second = new(new SectionId("same"), []);

        Assert.Throws<InvalidOperationException>(
            () => new Book(new BookId("book"), new BookMetadata("Title"), [first, second]));
    }

    [Fact]
    public void BookRejectsDuplicateResourceIds()
    {
        ReadingSection section = new(new SectionId("s1"), []);
        BookResource first = new(new ResourceId("r"), ResourceKind.Image, "image/png");
        BookResource second = new(new ResourceId("r"), ResourceKind.Image, "image/jpeg");

        Assert.Throws<InvalidOperationException>(
            () => new Book(new BookId("book"), new BookMetadata("Title"), [section], resources: [first, second]));
    }

    [Fact]
    public void BookRejectsMissingImageResource()
    {
        ImageBlock image = new(new BlockId("img"), new ResourceId("missing"));
        ReadingSection section = new(new SectionId("s1"), [image]);

        Assert.Throws<InvalidOperationException>(
            () => new Book(new BookId("book"), new BookMetadata("Title"), [section]));
    }

    [Fact]
    public void BookRejectsImagePointingToNonImageResource()
    {
        ImageBlock image = new(new BlockId("img"), new ResourceId("r1"));
        ReadingSection section = new(new SectionId("s1"), [image]);
        BookResource resource = new(new ResourceId("r1"), ResourceKind.Stylesheet, "text/css");

        Assert.Throws<InvalidOperationException>(
            () => new Book(new BookId("book"), new BookMetadata("Title"), [section], resources: [resource]));
    }

    [Fact]
    public void BookAcceptsResolvedImageResource()
    {
        ImageBlock image = new(new BlockId("img"), new ResourceId("r1"));
        ReadingSection section = new(new SectionId("s1"), [image]);
        BookResource resource = new(new ResourceId("r1"), ResourceKind.Image, "image/png");

        Book book = new(new BookId("book"), new BookMetadata("Title"), [section], resources: [resource]);

        Assert.Single(book.Resources);
    }

    [Fact]
    public void BookValidatesTocTargets()
    {
        ReadingSection section = CreateTextSection("s1", "p1", "hello");
        NavigationItem invalid = new("Missing", ReadingLocation.AtSectionStart(new SectionId("missing")));

        Assert.Throws<InvalidOperationException>(
            () => new Book(
                new BookId("book"),
                new BookMetadata("Title"),
                [section],
                new TableOfContents([invalid])));
    }

    [Fact]
    public void BookValidatesNestedTocTargets()
    {
        ReadingSection section = CreateTextSection("s1", "p1", "hello");
        NavigationItem child = new("Child", ReadingLocation.AtSectionStart(new SectionId("missing")));
        NavigationItem root = new("Root", ReadingLocation.AtSectionStart(new SectionId("s1")), [child]);

        Assert.Throws<InvalidOperationException>(
            () => new Book(
                new BookId("book"),
                new BookMetadata("Title"),
                [section],
                new TableOfContents([root])));
    }

    [Fact]
    public void BookValidatesInternalHyperlinkTargets()
    {
        ReadingLocation missing = ReadingLocation.AtBlockStart(new SectionId("s1"), new BlockId("missing"));
        HyperlinkSpan link = new(new InternalLinkTarget(missing), [new TextRun("go")]);
        ParagraphBlock paragraph = new(new BlockId("p1"), [link]);
        ReadingSection section = new(new SectionId("s1"), [paragraph]);

        Assert.Throws<InvalidOperationException>(
            () => new Book(new BookId("book"), new BookMetadata("Title"), [section]));
    }

    [Fact]
    public void ContainsLocationAcceptsUtf16OffsetAtTextEnd()
    {
        ReadingSection section = CreateTextSection("s1", "p1", "A😀B");
        Book book = new(new BookId("book"), new BookMetadata("Title"), [section]);
        ReadingLocation location = new(new SectionId("s1"), new BlockId("p1"), "A😀B".Length);

        Assert.True(book.ContainsLocation(location));
        Assert.Equal(4, location.CharacterOffset);
    }

    [Fact]
    public void ContainsLocationRejectsOffsetBeyondLogicalText()
    {
        ReadingSection section = CreateTextSection("s1", "p1", "hello");
        Book book = new(new BookId("book"), new BookMetadata("Title"), [section]);

        Assert.False(book.ContainsLocation(new ReadingLocation(new SectionId("s1"), new BlockId("p1"), 6)));
    }

    [Fact]
    public void BookSnapshotsReadingOrder()
    {
        List<ReadingSection> sections = [CreateTextSection("s1", "p1", "hello")];
        Book book = new(new BookId("book"), new BookMetadata("Title"), sections);

        sections.Add(CreateTextSection("s2", "p2", "world"));

        Assert.Single(book.ReadingOrder);
    }

    private static ReadingSection CreateTextSection(string sectionId, string blockId, string text)
    {
        ParagraphBlock paragraph = new(new BlockId(blockId), [new TextRun(text)]);
        return new ReadingSection(new SectionId(sectionId), [paragraph]);
    }
}
