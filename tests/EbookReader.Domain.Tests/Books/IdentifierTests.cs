namespace EbookReader.Domain.Tests.Books;

public sealed class IdentifierTests
{
    [Fact]
    public void BookIdRejectsWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new BookId("   "));
    }

    [Fact]
    public void SectionIdTrimsValue()
    {
        SectionId id = new(" chapter-1 ");

        Assert.Equal("chapter-1", id.Value);
    }

    [Fact]
    public void BlockIdUsesValueEquality()
    {
        Assert.Equal(new BlockId("p1"), new BlockId("p1"));
    }

    [Fact]
    public void ResourceIdUsesValueEquality()
    {
        Assert.Equal(new ResourceId("cover"), new ResourceId("cover"));
    }

    [Fact]
    public void BookIdentifierKeepsOptionalScheme()
    {
        BookIdentifier identifier = new("9780000000000", "isbn");

        Assert.Equal("9780000000000", identifier.Value);
        Assert.Equal("isbn", identifier.Scheme);
    }
}
