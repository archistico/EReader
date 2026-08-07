namespace EbookReader.Domain.Tests.Books;

public sealed class ReadingSectionTests
{
    [Fact]
    public void SectionSnapshotsBlocks()
    {
        List<ContentBlock> blocks = [new ParagraphBlock(new BlockId("p1"))];
        ReadingSection section = new(new SectionId("s1"), blocks);

        blocks.Add(new ParagraphBlock(new BlockId("p2")));

        Assert.Single(section.Blocks);
    }

    [Fact]
    public void SectionRejectsDuplicateBlockIds()
    {
        ContentBlock[] blocks =
        [
            new ParagraphBlock(new BlockId("same")),
            new ThematicBreakBlock(new BlockId("same")),
        ];

        Assert.Throws<ArgumentException>(() => new ReadingSection(new SectionId("s1"), blocks));
    }

    [Fact]
    public void SectionCanBeSupplementary()
    {
        ReadingSection section = new(
            new SectionId("notes"),
            [],
            "Notes",
            ReadingSectionRole.Supplementary);

        Assert.Equal(ReadingSectionRole.Supplementary, section.Role);
    }

    [Fact]
    public void SectionRejectsUndefinedRole()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReadingSection(new SectionId("s1"), [], role: (ReadingSectionRole)999));
    }

    [Fact]
    public void FindBlockUsesStableIdentifier()
    {
        ParagraphBlock paragraph = new(new BlockId("p1"), [new TextRun("text")]);
        ReadingSection section = new(new SectionId("s1"), [paragraph]);

        Assert.Same(paragraph, section.FindBlock(new BlockId("p1")));
    }
}
