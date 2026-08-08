using System.Collections.ObjectModel;
using System.Globalization;

namespace EbookReader.Cli.Configuration;

/// <summary>
/// Case-sensitive printable aliases for reader commands. Terminal special keys remain fixed
/// escape hatches and are deliberately not represented here.
/// </summary>
public sealed class ReaderKeymap
{
    private static readonly ReadOnlyDictionary<ReaderCommand, string> DefaultBindings =
        new(new Dictionary<ReaderCommand, string>
        {
            [ReaderCommand.PreviousLine] = "k",
            [ReaderCommand.NextLine] = "j",
            [ReaderCommand.PreviousPage] = "h",
            [ReaderCommand.NextPage] = "l",
            [ReaderCommand.PreviousChapter] = "[",
            [ReaderCommand.NextChapter] = "]",
            [ReaderCommand.ChapterStart] = "g",
            [ReaderCommand.ChapterEnd] = "G",
            [ReaderCommand.ToggleToc] = "t",
            [ReaderCommand.Search] = "/",
            [ReaderCommand.NextSearchResult] = "n",
            [ReaderCommand.PreviousSearchResult] = "N",
            [ReaderCommand.ToggleBookmark] = "b",
            [ReaderCommand.OpenBookmarks] = "B",
            [ReaderCommand.ToggleMetadata] = "m",
            [ReaderCommand.CycleTheme] = "c",
            [ReaderCommand.Help] = "?",
            [ReaderCommand.Quit] = "q",
            [ReaderCommand.DeleteBookmark] = "d",
        });

    private readonly ReadOnlyDictionary<ReaderCommand, string> _bindings;

    private ReaderKeymap(Dictionary<ReaderCommand, string> bindings)
    {
        _bindings = new ReadOnlyDictionary<ReaderCommand, string>(bindings);
    }

    public static ReaderKeymap Default { get; } = new(DefaultBindings.ToDictionary(pair => pair.Key, pair => pair.Value));

    public IReadOnlyDictionary<ReaderCommand, string> Bindings => _bindings;

    public string GetBinding(ReaderCommand command) => _bindings[command];

    public bool Matches(ReaderCommand command, string? printableText) =>
        string.Equals(GetBinding(command), printableText, StringComparison.Ordinal);

    public static ReaderKeymap Create(IReadOnlyDictionary<ReaderCommand, string>? overrides)
    {
        Dictionary<ReaderCommand, string> bindings = DefaultBindings.ToDictionary(pair => pair.Key, pair => pair.Value);
        if (overrides is not null)
        {
            foreach ((ReaderCommand command, string binding) in overrides)
            {
                if (!Enum.IsDefined(command))
                {
                    throw new ArgumentException($"Comando keymap sconosciuto: {command}.", nameof(overrides));
                }

                ValidateBinding(binding, command);
                bindings[command] = binding;
            }
        }

        string? duplicate = bindings
            .GroupBy(pair => pair.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .FirstOrDefault();
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Il tasto stampabile '{duplicate}' è assegnato a più comandi.",
                nameof(overrides));
        }

        return new ReaderKeymap(bindings);
    }

    private static void ValidateBinding(string binding, ReaderCommand command)
    {
        ArgumentException.ThrowIfNullOrEmpty(binding);
        int[] textElements = StringInfo.ParseCombiningCharacters(binding);
        if (textElements.Length != 1
            || binding.All(char.IsWhiteSpace)
            || binding.Any(char.IsControl))
        {
            throw new ArgumentException(
                $"Il binding per {command} deve essere un singolo grapheme stampabile non-spazio.",
                nameof(binding));
        }
    }
}
