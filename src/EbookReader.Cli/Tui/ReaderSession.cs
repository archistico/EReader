using System.Collections.ObjectModel;
using EbookReader.Application.Reading;
using EbookReader.Application.Search;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Navigation;
using EbookReader.Domain.Reading;
using EbookReader.Layout;

namespace EbookReader.Cli.Tui;

/// <summary>
/// Testable reader state whose durable position is always a ReadingLocation.
/// The BookLayout can be rebuilt for a new viewport without changing that logical position.
/// </summary>
public sealed class ReaderSession
{
    private readonly Book _book;
    private readonly ReadOnlyCollection<ReaderTocEntry> _tocEntries;
    private readonly ReadOnlyCollection<ReaderMetadataEntry> _metadataEntries;
    private BookSearchResultSet? _searchResults;
    private int _searchMatchIndex = -1;

    public ReaderSession(Book book, LayoutViewport viewport, ReadingLocation? initialLocation = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(viewport);

        if (initialLocation is not null && !book.ContainsLocation(initialLocation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialLocation),
                initialLocation,
                "La posizione iniziale non appartiene al libro.");
        }

        _book = book;
        _tocEntries = FlattenTableOfContents(book.TableOfContents);
        _metadataEntries = BuildMetadataEntries(book);
        Layout = DeterministicLayoutEngine.Layout(book, viewport);
        Location = initialLocation ?? InitialLocation(book);
    }

    public BookLayout Layout { get; private set; }

    public ReadingLocation Location { get; private set; }

    public bool HasReadableContent => Layout.Pages.Any(page => page.Lines.Any(line => line.StartLocation is not null));

    public LayoutPosition? Position => HasReadableContent
        ? LayoutLocationResolver.Locate(_book, Layout, Location)
        : null;

    public LayoutPage CurrentPage => Layout.Pages[PageNumber - 1];

    public int PageNumber => Position?.PageNumber ?? 1;

    public int PageCount => Layout.Pages.Count;

    public ReadOnlyCollection<ReaderTocEntry> TocEntries => _tocEntries;

    public bool HasTableOfContents => _tocEntries.Count > 0;

    public ReadOnlyCollection<ReaderMetadataEntry> MetadataEntries => _metadataEntries;

    public string? SearchQuery => _searchResults?.Query;

    public int SearchMatchCount => _searchResults?.Matches.Count ?? 0;

    public int CurrentSearchMatchNumber => _searchMatchIndex < 0 ? 0 : _searchMatchIndex + 1;

    public bool SearchResultsTruncated => _searchResults?.IsTruncated ?? false;

    /// <summary>
    /// Returns the navigable TOC entry nearest to, but not after, the current logical reading location.
    /// Pure grouping nodes are never returned as the suggested selection.
    /// </summary>
    public int SuggestedTocEntryIndex
    {
        get
        {
            int firstNavigable = -1;
            int best = -1;

            for (int index = 0; index < _tocEntries.Count; index++)
            {
                ReadingLocation? target = _tocEntries[index].Target;
                if (target is null)
                {
                    continue;
                }

                firstNavigable = firstNavigable < 0 ? index : firstNavigable;
                if (CompareLocations(target, Location) <= 0)
                {
                    best = index;
                }
            }

            return best >= 0 ? best : firstNavigable;
        }
    }

    public string BookTitle => _book.Metadata.Title;

    public string AuthorLine
    {
        get
        {
            string[] authors = _book.Metadata.Contributors
                .Where(contributor => contributor.Role == ContributorRole.Author)
                .Select(contributor => contributor.Name)
                .ToArray();

            return authors.Length == 0 ? string.Empty : string.Join(", ", authors);
        }
    }

    public string CurrentSectionTitle =>
        _book.FindSection(Location.SectionId)?.Title
        ?? string.Empty;

    public int CurrentPrimarySectionNumber
    {
        get
        {
            ReadingSection[] primary = _book.ReadingOrder
                .Where(section => section.Role == ReadingSectionRole.Primary)
                .ToArray();

            int index = Array.FindIndex(primary, section => section.Id == Location.SectionId);
            return index < 0 ? 0 : index + 1;
        }
    }

    public int PrimarySectionCount => _book.ReadingOrder.Count(section => section.Role == ReadingSectionRole.Primary);

    public string RenderCurrentPage() =>
        string.Join(Environment.NewLine, CurrentPage.Lines.Select(line => line.Text));

    /// <summary>
    /// Renders a sliding viewport beginning at the visual line that contains the current logical location.
    /// Unlike RenderCurrentPage, this makes single-line navigation immediately visible while keeping
    /// ReadingLocation as the only durable reader position. Synthetic spacing lines are rendered but are
    /// never themselves navigation destinations.
    /// </summary>
    public string RenderCurrentViewport()
    {
        if (!HasReadableContent || Position is not LayoutPosition position)
        {
            return string.Empty;
        }

        int remaining = Layout.Viewport.Height;
        List<string> lines = [];
        int pageIndex = position.PageNumber - 1;
        int lineIndex = position.LineIndex;

        while (pageIndex < Layout.Pages.Count && remaining > 0)
        {
            LayoutPage page = Layout.Pages[pageIndex];
            for (; lineIndex < page.Lines.Count && remaining > 0; lineIndex++)
            {
                lines.Add(page.Lines[lineIndex].Text);
                remaining--;
            }

            pageIndex++;
            lineIndex = 0;
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Rebuilds the ephemeral layout for a new viewport while preserving the exact logical ReadingLocation.
    /// Returns false when the viewport is unchanged and no reflow is required.
    /// </summary>
    public bool Reflow(LayoutViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(viewport);

        if (Layout.Viewport == viewport)
        {
            return false;
        }

        ReadingLocation logicalLocation = Location;
        Layout = DeterministicLayoutEngine.Layout(_book, viewport);
        Location = logicalLocation;
        return true;
    }

    public bool NextLine() => HasReadableContent && Move(LayoutNavigator.NextLine(_book, Layout, Location));

    public bool PreviousLine() => HasReadableContent && Move(LayoutNavigator.PreviousLine(_book, Layout, Location));

    public bool NextPage() => HasReadableContent && Move(LayoutNavigator.NextPage(_book, Layout, Location));

    public bool PreviousPage() => HasReadableContent && Move(LayoutNavigator.PreviousPage(_book, Layout, Location));

    public bool NextChapter() => Move(LogicalReadingNavigator.NextChapter(_book, Location));

    public bool PreviousChapter() => Move(LogicalReadingNavigator.PreviousChapter(_book, Location));

    public bool ChapterStart() => Move(LogicalReadingNavigator.ChapterStart(_book, Location));

    public bool ChapterEnd() => Move(LogicalReadingNavigator.ChapterEnd(_book, Location));

    /// <summary>
    /// Searches logical Domain text before layout. The first selected result is the first match that is
    /// not before the current ReadingLocation; when no such match exists the search wraps to the first hit.
    /// </summary>
    public bool Search(string query)
    {
        BookSearchResultSet results = BookTextSearch.Search(_book, query);
        _searchResults = results;

        if (results.Matches.Count == 0)
        {
            _searchMatchIndex = -1;
            return false;
        }

        int firstAtOrAfter = -1;
        for (int index = 0; index < results.Matches.Count; index++)
        {
            if (CompareLocations(results.Matches[index].Location, Location) >= 0)
            {
                firstAtOrAfter = index;
                break;
            }
        }

        _searchMatchIndex = firstAtOrAfter >= 0 ? firstAtOrAfter : 0;
        Location = results.Matches[_searchMatchIndex].Location;
        return true;
    }

    public bool NextSearchResult() => MoveSearchResult(1);

    public bool PreviousSearchResult() => MoveSearchResult(-1);

    public bool NavigateToTocEntry(int index)
    {
        if ((uint)index >= (uint)_tocEntries.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return Move(_tocEntries[index].Target);
    }

    public int FindAdjacentNavigableTocEntry(int currentIndex, int direction)
    {
        if (direction is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        if (_tocEntries.Count == 0)
        {
            return -1;
        }

        if (currentIndex < -1 || currentIndex >= _tocEntries.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(currentIndex));
        }

        int index = currentIndex;
        while (true)
        {
            index += direction;
            if (index < 0 || index >= _tocEntries.Count)
            {
                return currentIndex;
            }

            if (_tocEntries[index].CanNavigate)
            {
                return index;
            }
        }
    }


    private static ReadOnlyCollection<ReaderMetadataEntry> BuildMetadataEntries(Book book)
    {
        List<ReaderMetadataEntry> entries =
        [
            new("Titolo", book.Metadata.Title),
        ];

        AddOptional(entries, "Sottotitolo", book.Metadata.Subtitle);

        foreach (BookContributor contributor in book.Metadata.Contributors)
        {
            string value = string.IsNullOrWhiteSpace(contributor.SortName)
                ? contributor.Name
                : $"{contributor.Name} (ordinamento: {contributor.SortName})";
            entries.Add(new ReaderMetadataEntry(ContributorLabel(contributor.Role), value));
        }

        if (book.Metadata.Languages.Count > 0)
        {
            entries.Add(new ReaderMetadataEntry("Lingue", string.Join(", ", book.Metadata.Languages)));
        }

        AddOptional(entries, "Editore", book.Metadata.Publisher);

        foreach (BookIdentifier identifier in book.Metadata.Identifiers)
        {
            string label = string.IsNullOrWhiteSpace(identifier.Scheme)
                ? "Identificatore"
                : $"Identificatore ({identifier.Scheme})";
            entries.Add(new ReaderMetadataEntry(label, identifier.Value));
        }

        if (book.Metadata.Subjects.Count > 0)
        {
            entries.Add(new ReaderMetadataEntry("Argomenti", string.Join(", ", book.Metadata.Subjects)));
        }

        AddOptional(entries, "Diritti", book.Metadata.Rights);
        AddOptional(entries, "Descrizione", book.Metadata.Description);
        entries.Add(new ReaderMetadataEntry("Book ID", book.Id.Value));

        return entries.AsReadOnly();
    }

    private static void AddOptional(List<ReaderMetadataEntry> entries, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            entries.Add(new ReaderMetadataEntry(label, value));
        }
    }

    private static string ContributorLabel(ContributorRole role) => role switch
    {
        ContributorRole.Author => "Autore",
        ContributorRole.Editor => "Curatore",
        ContributorRole.Translator => "Traduttore",
        ContributorRole.Illustrator => "Illustratore",
        ContributorRole.Narrator => "Narratore",
        ContributorRole.Other => "Contributore",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Ruolo contributor non supportato."),
    };

    private static ReadOnlyCollection<ReaderTocEntry> FlattenTableOfContents(TableOfContents tableOfContents)
    {
        List<ReaderTocEntry> entries = [];
        AddItems(tableOfContents.Items, 0, entries);
        return entries.AsReadOnly();
    }

    private static void AddItems(
        IReadOnlyList<NavigationItem> items,
        int depth,
        List<ReaderTocEntry> destination)
    {
        foreach (NavigationItem item in items)
        {
            destination.Add(new ReaderTocEntry(item.Label, depth, item.Target));
            AddItems(item.Children, depth + 1, destination);
        }
    }

    private int CompareLocations(ReadingLocation left, ReadingLocation right)
    {
        int leftSection = FindSectionIndex(left.SectionId);
        int rightSection = FindSectionIndex(right.SectionId);
        int sectionComparison = leftSection.CompareTo(rightSection);
        if (sectionComparison != 0)
        {
            return sectionComparison;
        }

        ReadingSection section = _book.ReadingOrder[leftSection];
        int leftBlock = FindBlockIndex(section, left.BlockId);
        int rightBlock = FindBlockIndex(section, right.BlockId);
        int blockComparison = leftBlock.CompareTo(rightBlock);
        return blockComparison != 0
            ? blockComparison
            : left.CharacterOffset.CompareTo(right.CharacterOffset);
    }

    private int FindSectionIndex(SectionId sectionId)
    {
        for (int index = 0; index < _book.ReadingOrder.Count; index++)
        {
            if (_book.ReadingOrder[index].Id == sectionId)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Sezione TOC non presente nel libro: {sectionId}.");
    }

    private static int FindBlockIndex(ReadingSection section, BlockId? blockId)
    {
        if (blockId is null)
        {
            return -1;
        }

        for (int index = 0; index < section.Blocks.Count; index++)
        {
            if (section.Blocks[index].Id == blockId)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Blocco TOC non presente nella sezione {section.Id}: {blockId}.");
    }

    private bool MoveSearchResult(int direction)
    {
        if (direction is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        if (_searchResults is null || _searchResults.Matches.Count == 0)
        {
            return false;
        }

        int count = _searchResults.Matches.Count;
        _searchMatchIndex = (_searchMatchIndex + direction + count) % count;
        Location = _searchResults.Matches[_searchMatchIndex].Location;
        return true;
    }

    private bool Move(ReadingLocation? destination)
    {
        if (destination is null || destination == Location)
        {
            return false;
        }

        Location = destination;
        return true;
    }

    private static ReadingLocation InitialLocation(Book book)
    {
        ReadingSection section = book.ReadingOrder.FirstOrDefault(candidate => candidate.Role == ReadingSectionRole.Primary)
            ?? book.ReadingOrder[0];
        return ReadingLocation.AtSectionStart(section.Id);
    }
}
