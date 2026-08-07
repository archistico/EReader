using EbookReader.Layout;

namespace EbookReader.Layout.Tests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void AssemblyHasExpectedName()
    {
        Assert.Equal("EbookReader.Layout", typeof(LayoutAssembly).Assembly.GetName().Name);
    }
}
