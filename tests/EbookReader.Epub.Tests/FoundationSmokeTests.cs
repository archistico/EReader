using EbookReader.Epub;

namespace EbookReader.Epub.Tests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void AssemblyHasExpectedName()
    {
        Assert.Equal("EbookReader.Epub", typeof(EpubAssembly).Assembly.GetName().Name);
    }
}
