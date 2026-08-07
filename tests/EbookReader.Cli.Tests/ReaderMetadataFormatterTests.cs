using EbookReader.Cli.Tui;
using EbookReader.Layout;

namespace EbookReader.Cli.Tests;

public sealed class ReaderMetadataFormatterTests
{
    [Fact]
    public void WrapsLongMetadataWithoutExceedingTerminalCellWidth()
    {
        ReaderMetadataEntry[] entries =
        [
            new("Descrizione", "alpha beta gamma delta epsilon zeta"),
        ];

        string[] lines = ReaderMetadataFormatter.Format(entries, 20);

        Assert.True(lines.Length > 1);
        Assert.All(lines, line => Assert.True(TerminalCellWidth.Measure(line) <= 20));
        Assert.StartsWith("Descrizione:", lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void WrapsWideUnicodeByTerminalCells()
    {
        ReaderMetadataEntry[] entries =
        [
            new("Titolo", "日本語 😀 EPUB terminale"),
        ];

        string[] lines = ReaderMetadataFormatter.Format(entries, 16);

        Assert.All(lines, line => Assert.True(TerminalCellWidth.Measure(line) <= 16));
        Assert.Contains(lines, line => line.Contains("日本", StringComparison.Ordinal));
    }

    [Fact]
    public void PreservesExplicitMetadataLineBreaks()
    {
        ReaderMetadataEntry[] entries =
        [
            new("Descrizione", "prima riga\nseconda riga"),
        ];

        string[] lines = ReaderMetadataFormatter.Format(entries, 40);

        Assert.Equal(2, lines.Length);
        Assert.Contains("prima riga", lines[0], StringComparison.Ordinal);
        Assert.Contains("seconda riga", lines[1], StringComparison.Ordinal);
    }
}
