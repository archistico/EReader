using EbookReader.Cli;

namespace EbookReader.Cli.Tests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void MilestoneIsM36()
    {
        Assert.Equal("M3.6", CliEntryPoint.Milestone);
    }

    [Fact]
    public void TerminalGuiDependencyIsResolvable()
    {
        string version = CliEntryPoint.GetTerminalGuiVersion();

        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.NotEqual("unknown", version);
    }
}
