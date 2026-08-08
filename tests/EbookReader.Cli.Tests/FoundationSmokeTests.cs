using EbookReader.Cli;

namespace EbookReader.Cli.Tests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void MilestoneIsM310()
    {
        Assert.Equal("M3.11", CliEntryPoint.Milestone);
    }

    [Fact]
    public void TerminalGuiDependencyIsResolvable()
    {
        string version = CliEntryPoint.GetTerminalGuiVersion();

        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.NotEqual("unknown", version);
    }
}
