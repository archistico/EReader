using EbookReader.Domain;

namespace EbookReader.Domain.Tests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void AssemblyHasExpectedName()
    {
        Assert.Equal("EbookReader.Domain", typeof(DomainAssembly).Assembly.GetName().Name);
    }
}
