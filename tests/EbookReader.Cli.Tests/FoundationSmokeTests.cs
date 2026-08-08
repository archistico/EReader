using EbookReader.Cli;

namespace EbookReader.Cli.Tests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void MilestoneIsM313()
    {
        Assert.Equal("M3.13", CliEntryPoint.Milestone);
    }

    [Fact]
    public void ProductDisplayNameCreditsEmilieRollandin()
    {
        Assert.Equal("EReader by Emilie Rollandin", CliEntryPoint.DisplayName);
    }

    [Fact]
    public void TerminalGuiDependencyIsResolvable()
    {
        string version = CliEntryPoint.GetTerminalGuiVersion();

        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.NotEqual("unknown", version);
    }
}
