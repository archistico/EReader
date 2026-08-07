using EbookReader.Application;

namespace EbookReader.Application.Tests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void AssemblyHasExpectedName()
    {
        Assert.Equal("EbookReader.Application", typeof(ApplicationAssembly).Assembly.GetName().Name);
    }
}
