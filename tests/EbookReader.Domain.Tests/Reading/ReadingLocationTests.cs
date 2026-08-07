namespace EbookReader.Domain.Tests.Reading;

public sealed class ReadingLocationTests
{
    [Fact]
    public void SectionStartHasNoBlockAndZeroOffset()
    {
        ReadingLocation location = ReadingLocation.AtSectionStart(new SectionId("s1"));

        Assert.Null(location.BlockId);
        Assert.Equal(0, location.CharacterOffset);
    }

    [Fact]
    public void BlockStartHasZeroOffset()
    {
        ReadingLocation location = ReadingLocation.AtBlockStart(new SectionId("s1"), new BlockId("p1"));

        Assert.Equal(new BlockId("p1"), location.BlockId);
        Assert.Equal(0, location.CharacterOffset);
    }

    [Fact]
    public void LocationRejectsNegativeOffset()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReadingLocation(new SectionId("s1"), new BlockId("p1"), -1));
    }

    [Fact]
    public void SectionOnlyLocationRejectsNonzeroOffset()
    {
        Assert.Throws<ArgumentException>(() => new ReadingLocation(new SectionId("s1"), null, 1));
    }

    [Fact]
    public void LocationsUseValueEquality()
    {
        ReadingLocation first = new(new SectionId("s1"), new BlockId("p1"), 5);
        ReadingLocation second = new(new SectionId("s1"), new BlockId("p1"), 5);

        Assert.Equal(first, second);
    }
}
