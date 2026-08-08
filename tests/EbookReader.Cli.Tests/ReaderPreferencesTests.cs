using EbookReader.Cli.Configuration;

namespace EbookReader.Cli.Tests;

public sealed class ReaderPreferencesTests
{
    [Fact]
    public void MissingFileReturnsDefaultPreferences()
    {
        string path = NewPath();
        JsonReaderPreferencesStore store = new(path);

        ReaderPreferences preferences = store.Load();

        Assert.Equal(ReaderThemeIds.SemanticDark, preferences.ThemeId);
        Assert.Equal("j", preferences.Keymap.GetBinding(ReaderCommand.NextLine));
        Assert.Equal("k", preferences.Keymap.GetBinding(ReaderCommand.PreviousLine));
    }

    [Fact]
    public void SaveAndLoadRoundTripsThemeAndCustomPrintableBindings()
    {
        string path = NewPath();
        try
        {
            ReaderKeymap keymap = ReaderKeymap.Create(new Dictionary<ReaderCommand, string>
            {
                [ReaderCommand.NextLine] = "x",
                [ReaderCommand.PreviousLine] = "z",
            });
            ReaderPreferences expected = new(ReaderThemeIds.PaperLight, keymap);
            JsonReaderPreferencesStore store = new(path);

            store.Save(expected);
            ReaderPreferences actual = store.Load();

            Assert.Equal(ReaderThemeIds.PaperLight, actual.ThemeId);
            Assert.Equal("x", actual.Keymap.GetBinding(ReaderCommand.NextLine));
            Assert.Equal("z", actual.Keymap.GetBinding(ReaderCommand.PreviousLine));
            Assert.Equal("b", actual.Keymap.GetBinding(ReaderCommand.ToggleBookmark));
        }
        finally
        {
            DeleteFileAndParent(path);
        }
    }

    [Fact]
    public void PartialJsonKeymapMergesWithDefaults()
    {
        string path = NewPath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(
                path,
                """
                {
                  "schemaVersion": 1,
                  "theme": "monochrome",
                  "keymap": {
                    "NextLine": "x"
                  }
                }
                """);

            ReaderPreferences preferences = new JsonReaderPreferencesStore(path).Load();

            Assert.Equal(ReaderThemeIds.Monochrome, preferences.ThemeId);
            Assert.Equal("x", preferences.Keymap.GetBinding(ReaderCommand.NextLine));
            Assert.Equal("k", preferences.Keymap.GetBinding(ReaderCommand.PreviousLine));
        }
        finally
        {
            DeleteFileAndParent(path);
        }
    }

    [Fact]
    public void DuplicatePrintableBindingsAreRejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            ReaderKeymap.Create(new Dictionary<ReaderCommand, string>
            {
                [ReaderCommand.NextLine] = "k",
            }));

        Assert.Contains("più comandi", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MultiGraphemeBindingsAreRejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            ReaderKeymap.Create(new Dictionary<ReaderCommand, string>
            {
                [ReaderCommand.NextLine] = "jj",
            }));

        Assert.Contains("singolo grapheme", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownConfigCommandIsRejected()
    {
        string path = NewPath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(
                path,
                """
                {
                  "schemaVersion": 1,
                  "theme": "semantic-dark",
                  "keymap": {
                    "Teleport": "x"
                  }
                }
                """);

            Assert.Throws<InvalidDataException>(() => new JsonReaderPreferencesStore(path).Load());
        }
        finally
        {
            DeleteFileAndParent(path);
        }
    }

    [Fact]
    public void UnsupportedConfigSchemaIsRejected()
    {
        string path = NewPath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{\"schemaVersion\":99,\"theme\":\"semantic-dark\"}");

            Assert.Throws<InvalidDataException>(() => new JsonReaderPreferencesStore(path).Load());
        }
        finally
        {
            DeleteFileAndParent(path);
        }
    }

    private static string NewPath() =>
        Path.Combine(Path.GetTempPath(), $"ereader-config-tests-{Guid.NewGuid():N}", "config.json");

    private static void DeleteFileAndParent(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
