namespace EbookReader.Domain.Tests.Books;

public sealed class BookMetadataTests
{
    [Fact]
    public void MetadataRequiresTitle()
    {
        Assert.Throws<ArgumentException>(() => new BookMetadata(" "));
    }

    [Fact]
    public void MetadataNormalizesOptionalText()
    {
        BookMetadata metadata = new(" Title ", subtitle: " ", publisher: " Publisher ");

        Assert.Equal("Title", metadata.Title);
        Assert.Null(metadata.Subtitle);
        Assert.Equal("Publisher", metadata.Publisher);
    }

    [Fact]
    public void MetadataSnapshotsCollections()
    {
        List<string> languages = ["it"];
        BookMetadata metadata = new("Titolo", languages: languages);

        languages.Add("en");

        Assert.Equal(["it"], metadata.Languages);
    }

    [Fact]
    public void ContributorCapturesNeutralRole()
    {
        BookContributor contributor = new("Umberto Eco", ContributorRole.Author, "Eco, Umberto");

        Assert.Equal(ContributorRole.Author, contributor.Role);
        Assert.Equal("Eco, Umberto", contributor.SortName);
    }

    [Fact]
    public void ContributorRejectsUndefinedRole()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BookContributor("Name", (ContributorRole)999));
    }

    [Fact]
    public void MetadataRejectsBlankLanguageEntry()
    {
        Assert.Throws<ArgumentException>(() => new BookMetadata("Titolo", languages: ["it", " "]));
    }
}
