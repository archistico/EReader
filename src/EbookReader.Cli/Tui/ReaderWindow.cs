using System.Globalization;
using System.Text;
using EbookReader.Cli.Configuration;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EbookReader.Cli.Tui;

/// <summary>
/// Thin Terminal.Gui v2 adapter over ReaderSession. No EPUB parsing, wrapping, pagination or
/// logical-navigation rules are implemented in this view.
/// </summary>
internal sealed class ReaderWindow : Window
{
    private readonly ReaderSession _session;
    private readonly ReaderKeymap _keymap;
    private static readonly string HorizontalRule = new('─', 1024);

    private readonly Label _header;
    private readonly Label _headerSeparator;
    private readonly ReaderBodyView _body;
    private readonly Label _footerSeparator;
    private readonly Label _footer;
    private bool _helpVisible;
    private bool _tocVisible;
    private bool _metadataVisible;
    private bool _bookmarksVisible;
    private bool _searchInputVisible;
    private readonly StringBuilder _searchInput = new();
    private int _tocSelectedIndex = -1;
    private int _tocScrollOffset;
    private int _metadataScrollOffset;
    private int _bookmarkSelectedIndex = -1;
    private int _bookmarkScrollOffset;
    private bool _synchronizingViewport;
    private int _themeIndex;

    public ReaderWindow(ReaderSession session, ReaderPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(preferences);
        _session = session;
        _keymap = preferences.Keymap;
        _themeIndex = ReaderThemeCatalog.IndexOfId(preferences.ThemeId);

        Title = "EReader";
        Width = Dim.Fill();
        Height = Dim.Fill();
        SetScheme(ReaderColorPalette.ChromeScheme);

        _header = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
        };
        _header.SetScheme(ReaderColorPalette.PlainScheme);

        _headerSeparator = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = 1,
            Text = HorizontalRule,
        };
        _headerSeparator.SetScheme(ReaderColorPalette.ChromeScheme);

        _body = new ReaderBodyView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };

        _footerSeparator = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(),
            Height = 1,
            Text = HorizontalRule,
        };
        _footerSeparator.SetScheme(ReaderColorPalette.ChromeScheme);

        _footer = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
        };
        _footer.SetScheme(ReaderColorPalette.PlainScheme);

        _body.ViewportChanged += (_, _) => SynchronizeViewport();

        Add(_header, _headerSeparator, _body, _footerSeparator, _footer);
        ApplyTheme(ReaderThemeCatalog.All[_themeIndex]);
        RefreshReader();
    }

    public string CurrentThemeId => ReaderThemeCatalog.All[_themeIndex].Id;

    protected override bool OnKeyDown(Key key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_searchInputVisible)
        {
            return HandleSearchInputKey(key);
        }

        if (Matches(ReaderCommand.CycleTheme, key))
        {
            CycleTheme();
            return true;
        }

        if (Matches(ReaderCommand.Search, key))
        {
            OpenSearchInput();
            return true;
        }

        if (key == Key.F1 || Matches(ReaderCommand.Help, key))
        {
            ToggleHelp();
            return true;
        }

        if (Matches(ReaderCommand.Quit, key))
        {
            App?.RequestStop();
            return true;
        }

        if (key == Key.Esc)
        {
            if (_bookmarksVisible)
            {
                CloseBookmarks();
            }
            else if (_metadataVisible)
            {
                CloseMetadata();
            }
            else if (_tocVisible)
            {
                CloseToc();
            }
            else if (_helpVisible)
            {
                _helpVisible = false;
                RefreshReader();
            }
            else
            {
                App?.RequestStop();
            }

            return true;
        }

        if (key == Key.Tab || Matches(ReaderCommand.ToggleToc, key))
        {
            ToggleToc();
            return true;
        }

        if (Matches(ReaderCommand.ToggleMetadata, key))
        {
            ToggleMetadata();
            return true;
        }

        if (Matches(ReaderCommand.OpenBookmarks, key))
        {
            ToggleBookmarks();
            return true;
        }

        if (_helpVisible)
        {
            return true;
        }

        if (_tocVisible)
        {
            return HandleTocKey(key);
        }

        if (_metadataVisible)
        {
            return HandleMetadataKey(key);
        }

        if (_bookmarksVisible)
        {
            return HandleBookmarkKey(key);
        }

        if (Matches(ReaderCommand.ToggleBookmark, key))
        {
            _session.ToggleBookmark();
            RefreshReader();
            return true;
        }

        if (Matches(ReaderCommand.NextSearchResult, key))
        {
            Navigate(_session.NextSearchResult);
            return true;
        }

        if (Matches(ReaderCommand.PreviousSearchResult, key))
        {
            Navigate(_session.PreviousSearchResult);
            return true;
        }

        if (key == Key.CursorDown || Matches(ReaderCommand.NextLine, key))
        {
            Navigate(_session.NextLine);
            return true;
        }

        if (key == Key.CursorUp || Matches(ReaderCommand.PreviousLine, key))
        {
            Navigate(_session.PreviousLine);
            return true;
        }

        if (key == Key.PageDown || key == Key.Space || Matches(ReaderCommand.NextPage, key))
        {
            Navigate(_session.NextPage);
            return true;
        }

        if (key == Key.PageUp || Matches(ReaderCommand.PreviousPage, key))
        {
            Navigate(_session.PreviousPage);
            return true;
        }

        if (Matches(ReaderCommand.NextChapter, key))
        {
            Navigate(_session.NextChapter);
            return true;
        }

        if (Matches(ReaderCommand.PreviousChapter, key))
        {
            Navigate(_session.PreviousChapter);
            return true;
        }

        if (Matches(ReaderCommand.ChapterEnd, key))
        {
            Navigate(_session.ChapterEnd);
            return true;
        }

        if (Matches(ReaderCommand.ChapterStart, key))
        {
            Navigate(_session.ChapterStart);
            return true;
        }

        return base.OnKeyDown(key);
    }

    private bool HandleSearchInputKey(Key key)
    {
        if (key == Key.Esc)
        {
            _searchInputVisible = false;
            _searchInput.Clear();
            RefreshReader();
            return true;
        }

        if (key == Key.Enter)
        {
            string query = _searchInput.ToString();
            _searchInputVisible = false;
            _searchInput.Clear();

            if (!string.IsNullOrWhiteSpace(query))
            {
                _session.Search(query);
            }

            RefreshReader();
            return true;
        }

        if (key == Key.Backspace)
        {
            RemoveLastSearchTextElement();
            RefreshReader();
            return true;
        }

        string printable = key.GetPrintableText();
        if (!string.IsNullOrEmpty(printable)
            && printable.All(character => !char.IsControl(character))
            && _searchInput.Length + printable.Length <= global::EbookReader.Application.Search.BookTextSearch.MaximumQueryLength)
        {
            _searchInput.Append(printable);
            RefreshReader();
        }

        return true;
    }

    private bool HandleTocKey(Key key)
    {
        if (key == Key.CursorDown || Matches(ReaderCommand.NextLine, key))
        {
            MoveTocSelection(1);
            return true;
        }

        if (key == Key.CursorUp || Matches(ReaderCommand.PreviousLine, key))
        {
            MoveTocSelection(-1);
            return true;
        }

        if (key == Key.PageDown)
        {
            MoveTocSelectionByPage(1);
            return true;
        }

        if (key == Key.PageUp)
        {
            MoveTocSelectionByPage(-1);
            return true;
        }

        if (key == Key.Enter)
        {
            if (_tocSelectedIndex >= 0 && _session.NavigateToTocEntry(_tocSelectedIndex))
            {
                _tocVisible = false;
                _tocScrollOffset = 0;
                RefreshReader();
            }
            else if (_tocSelectedIndex >= 0 && _session.TocEntries[_tocSelectedIndex].CanNavigate)
            {
                // The selected target can already be the current location; Enter still closes the TOC.
                _tocVisible = false;
                _tocScrollOffset = 0;
                RefreshReader();
            }

            return true;
        }

        return true;
    }

    private bool HandleMetadataKey(Key key)
    {
        if (key == Key.CursorDown || Matches(ReaderCommand.NextLine, key))
        {
            MoveMetadataScroll(1);
            return true;
        }

        if (key == Key.CursorUp || Matches(ReaderCommand.PreviousLine, key))
        {
            MoveMetadataScroll(-1);
            return true;
        }

        if (key == Key.PageDown)
        {
            MoveMetadataScroll(Math.Max(_body.Viewport.Height - 1, 1));
            return true;
        }

        if (key == Key.PageUp)
        {
            MoveMetadataScroll(-Math.Max(_body.Viewport.Height - 1, 1));
            return true;
        }

        return true;
    }

    private bool HandleBookmarkKey(Key key)
    {
        if (key == Key.CursorDown || Matches(ReaderCommand.NextLine, key))
        {
            MoveBookmarkSelection(1);
            return true;
        }

        if (key == Key.CursorUp || Matches(ReaderCommand.PreviousLine, key))
        {
            MoveBookmarkSelection(-1);
            return true;
        }

        if (key == Key.PageDown)
        {
            MoveBookmarkSelectionByPage(1);
            return true;
        }

        if (key == Key.PageUp)
        {
            MoveBookmarkSelectionByPage(-1);
            return true;
        }

        if (key == Key.Enter)
        {
            if (_bookmarkSelectedIndex >= 0)
            {
                _session.NavigateToBookmark(_bookmarkSelectedIndex);
                CloseBookmarks();
            }

            return true;
        }

        if (Matches(ReaderCommand.DeleteBookmark, key))
        {
            DeleteSelectedBookmark();
            return true;
        }

        return true;
    }

    private void CycleTheme()
    {
        _themeIndex = (_themeIndex + 1) % ReaderThemeCatalog.All.Count;
        ApplyTheme(ReaderThemeCatalog.All[_themeIndex]);
        RefreshReader();
    }

    private void ApplyTheme(ReaderTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        SetScheme(theme.ChromeScheme);
        _header.SetScheme(theme.PlainScheme);
        _headerSeparator.SetScheme(theme.ChromeScheme);
        _body.ApplyTheme(theme);
        _footerSeparator.SetScheme(theme.ChromeScheme);
        _footer.SetScheme(theme.PlainScheme);
        SetNeedsDraw();
    }

    private void Navigate(Func<bool> movement)
    {
        if (movement())
        {
            RefreshReader();
        }
    }

    private bool Matches(ReaderCommand command, Key key) =>
        _keymap.Matches(command, key.GetPrintableText());

    private string Binding(ReaderCommand command) => _keymap.GetBinding(command);

    private void OpenSearchInput()
    {
        _helpVisible = false;
        _tocVisible = false;
        _metadataVisible = false;
        _bookmarksVisible = false;
        _searchInputVisible = true;
        _searchInput.Clear();
        RefreshReader();
    }

    private void RemoveLastSearchTextElement()
    {
        if (_searchInput.Length == 0)
        {
            return;
        }

        string current = _searchInput.ToString();
        int[] starts = StringInfo.ParseCombiningCharacters(current);
        _searchInput.Length = starts[^1];
    }

    private void ToggleHelp()
    {
        _helpVisible = !_helpVisible;
        if (_helpVisible)
        {
            _tocVisible = false;
            _metadataVisible = false;
            _bookmarksVisible = false;
        }

        RefreshReader();
    }

    private void ToggleToc()
    {
        if (_tocVisible)
        {
            CloseToc();
            return;
        }

        if (!_session.HasTableOfContents)
        {
            return;
        }

        _helpVisible = false;
        _metadataVisible = false;
        _bookmarksVisible = false;
        _tocVisible = true;
        _tocSelectedIndex = _session.SuggestedTocEntryIndex;
        _tocScrollOffset = 0;
        EnsureTocSelectionVisible();
        RefreshReader();
    }

    private void ToggleMetadata()
    {
        if (_metadataVisible)
        {
            CloseMetadata();
            return;
        }

        _helpVisible = false;
        _tocVisible = false;
        _bookmarksVisible = false;
        _metadataVisible = true;
        _metadataScrollOffset = 0;
        RefreshReader();
    }

    private void ToggleBookmarks()
    {
        if (_bookmarksVisible)
        {
            CloseBookmarks();
            return;
        }

        _helpVisible = false;
        _tocVisible = false;
        _metadataVisible = false;
        _bookmarksVisible = true;
        _bookmarkSelectedIndex = _session.SuggestedBookmarkIndex;
        _bookmarkScrollOffset = 0;
        EnsureBookmarkSelectionVisible();
        RefreshReader();
    }

    private void CloseBookmarks()
    {
        _bookmarksVisible = false;
        _bookmarkScrollOffset = 0;
        RefreshReader();
    }

    private void MoveBookmarkSelection(int direction)
    {
        int next = _session.FindAdjacentBookmark(_bookmarkSelectedIndex, direction);
        if (next == _bookmarkSelectedIndex)
        {
            return;
        }

        _bookmarkSelectedIndex = next;
        EnsureBookmarkSelectionVisible();
        RefreshReader();
    }

    private void MoveBookmarkSelectionByPage(int direction)
    {
        int steps = Math.Max(_body.Viewport.Height - 1, 1);
        for (int step = 0; step < steps; step++)
        {
            int next = _session.FindAdjacentBookmark(_bookmarkSelectedIndex, direction);
            if (next == _bookmarkSelectedIndex)
            {
                break;
            }

            _bookmarkSelectedIndex = next;
        }

        EnsureBookmarkSelectionVisible();
        RefreshReader();
    }

    private void DeleteSelectedBookmark()
    {
        if (_bookmarkSelectedIndex < 0 || _session.BookmarkCount == 0)
        {
            return;
        }

        _session.RemoveBookmark(_bookmarkSelectedIndex);
        if (_session.BookmarkCount == 0)
        {
            _bookmarkSelectedIndex = -1;
            _bookmarkScrollOffset = 0;
        }
        else
        {
            _bookmarkSelectedIndex = Math.Min(_bookmarkSelectedIndex, _session.BookmarkCount - 1);
            EnsureBookmarkSelectionVisible();
        }

        RefreshReader();
    }

    private void EnsureBookmarkSelectionVisible()
    {
        int height = Math.Max(_body.Viewport.Height, 1);
        if (_bookmarkSelectedIndex < 0)
        {
            _bookmarkScrollOffset = 0;
            return;
        }

        if (_bookmarkSelectedIndex < _bookmarkScrollOffset)
        {
            _bookmarkScrollOffset = _bookmarkSelectedIndex;
        }
        else if (_bookmarkSelectedIndex >= _bookmarkScrollOffset + height)
        {
            _bookmarkScrollOffset = _bookmarkSelectedIndex - height + 1;
        }

        _bookmarkScrollOffset = Math.Max(_bookmarkScrollOffset, 0);
    }

    private void CloseMetadata()
    {
        _metadataVisible = false;
        _metadataScrollOffset = 0;
        RefreshReader();
    }

    private void MoveMetadataScroll(int delta)
    {
        string[] lines = BuildMetadataLines();
        int visibleHeight = Math.Max(_body.Viewport.Height, 1);
        int maximumOffset = Math.Max(lines.Length - visibleHeight, 0);
        int next = Math.Clamp(_metadataScrollOffset + delta, 0, maximumOffset);
        if (next == _metadataScrollOffset)
        {
            return;
        }

        _metadataScrollOffset = next;
        RefreshReader();
    }

    private void CloseToc()
    {
        _tocVisible = false;
        _tocScrollOffset = 0;
        RefreshReader();
    }

    private void MoveTocSelection(int direction)
    {
        int next = _session.FindAdjacentNavigableTocEntry(_tocSelectedIndex, direction);
        if (next == _tocSelectedIndex)
        {
            return;
        }

        _tocSelectedIndex = next;
        EnsureTocSelectionVisible();
        RefreshReader();
    }

    private void MoveTocSelectionByPage(int direction)
    {
        int steps = Math.Max(_body.Viewport.Height - 1, 1);
        for (int step = 0; step < steps; step++)
        {
            int next = _session.FindAdjacentNavigableTocEntry(_tocSelectedIndex, direction);
            if (next == _tocSelectedIndex)
            {
                break;
            }

            _tocSelectedIndex = next;
        }

        EnsureTocSelectionVisible();
        RefreshReader();
    }

    private void EnsureTocSelectionVisible()
    {
        int height = Math.Max(_body.Viewport.Height, 1);
        if (_tocSelectedIndex < _tocScrollOffset)
        {
            _tocScrollOffset = _tocSelectedIndex;
        }
        else if (_tocSelectedIndex >= _tocScrollOffset + height)
        {
            _tocScrollOffset = _tocSelectedIndex - height + 1;
        }

        _tocScrollOffset = Math.Max(_tocScrollOffset, 0);
    }

    private string BuildToc()
    {
        EnsureTocSelectionVisible();
        int height = Math.Max(_body.Viewport.Height, 1);
        StringBuilder text = new();
        int end = Math.Min(_tocScrollOffset + height, _session.TocEntries.Count);

        for (int index = _tocScrollOffset; index < end; index++)
        {
            ReaderTocEntry entry = _session.TocEntries[index];
            string marker = index == _tocSelectedIndex ? "> " : "  ";
            text.Append(marker);
            text.Append(' ', entry.Depth * 2);
            text.Append(entry.Label);

            if (index + 1 < end)
            {
                text.AppendLine();
            }
        }

        return text.ToString();
    }

    private string BuildBookmarks()
    {
        if (_session.BookmarkCount == 0)
        {
            return "Nessun segnalibro. Premi Esc o B per tornare al libro.";
        }

        EnsureBookmarkSelectionVisible();
        int height = Math.Max(_body.Viewport.Height, 1);
        int end = Math.Min(_bookmarkScrollOffset + height, _session.BookmarkEntries.Count);
        StringBuilder text = new();

        for (int index = _bookmarkScrollOffset; index < end; index++)
        {
            ReaderBookmarkEntry entry = _session.BookmarkEntries[index];
            text.Append(index == _bookmarkSelectedIndex ? "> " : "  ");
            text.Append(index + 1);
            text.Append(". ");
            text.Append(entry.Label);

            if (index + 1 < end)
            {
                text.AppendLine();
            }
        }

        return text.ToString();
    }

    private string BuildMetadata()
    {
        string[] lines = BuildMetadataLines();
        int visibleHeight = Math.Max(_body.Viewport.Height, 1);
        int maximumOffset = Math.Max(lines.Length - visibleHeight, 0);
        _metadataScrollOffset = Math.Clamp(_metadataScrollOffset, 0, maximumOffset);

        return string.Join(
            Environment.NewLine,
            lines.Skip(_metadataScrollOffset).Take(visibleHeight));
    }

    private string[] BuildMetadataLines()
    {
        int width = Math.Max(_body.Viewport.Width, 2);
        return ReaderMetadataFormatter.Format(_session.MetadataEntries, width);
    }

    private void SynchronizeViewport()
    {
        if (_synchronizingViewport)
        {
            return;
        }

        try
        {
            _synchronizingViewport = true;
            global::EbookReader.Layout.LayoutViewport viewport = TerminalViewportFactory.CreateFromBodyViewport(
                _body.Viewport.Width,
                _body.Viewport.Height);

            if (_session.Reflow(viewport))
            {
                if (_tocVisible)
                {
                    EnsureTocSelectionVisible();
                }

                if (_bookmarksVisible)
                {
                    EnsureBookmarkSelectionVisible();
                }

                RefreshReader();
            }
        }
        finally
        {
            _synchronizingViewport = false;
        }
    }

    private void RefreshReader()
    {
        _header.Text = BuildHeader();
        if (_helpVisible)
        {
            _body.ShowPlainText(BuildHelp());
        }
        else if (_tocVisible)
        {
            _body.ShowPlainText(BuildToc());
        }
        else if (_metadataVisible)
        {
            _body.ShowPlainText(BuildMetadata());
        }
        else if (_bookmarksVisible)
        {
            _body.ShowPlainText(BuildBookmarks());
        }
        else
        {
            _body.ShowReaderLines(_session.GetCurrentViewportLines());
        }

        _footer.Text = _searchInputVisible
            ? $"Cerca: {_searchInput}_   Enter cerca   Esc annulla"
            : _helpVisible
                ? $"F1/{Binding(ReaderCommand.Help)}/Esc chiudi aiuto   {Binding(ReaderCommand.Quit)} esci"
                : _tocVisible
                    ? BuildTocFooter()
                    : _metadataVisible
                        ? BuildMetadataFooter()
                        : _bookmarksVisible
                            ? BuildBookmarkFooter()
                            : BuildNormalFooter();
    }

    private string BuildHeader()
    {
        string author = string.IsNullOrWhiteSpace(_session.AuthorLine) ? string.Empty : $" — {_session.AuthorLine}";
        if (_metadataVisible)
        {
            return $"{_session.BookTitle}{author}   Metadati   {_session.MetadataEntries.Count} campi";
        }

        if (_bookmarksVisible)
        {
            int selectedOrdinal = _bookmarkSelectedIndex < 0 ? 0 : _bookmarkSelectedIndex + 1;
            return $"{_session.BookTitle}{author}   Segnalibri   {selectedOrdinal}/{_session.BookmarkCount}";
        }

        if (_tocVisible)
        {
            int navigableCount = _session.TocEntries.Count(entry => entry.CanNavigate);
            int selectedOrdinal = _tocSelectedIndex < 0
                ? 0
                : _session.TocEntries.Take(_tocSelectedIndex + 1).Count(entry => entry.CanNavigate);
            return $"{_session.BookTitle}{author}   Indice   {selectedOrdinal}/{navigableCount}";
        }

        string chapter = _session.CurrentPrimarySectionNumber > 0
            ? $"Cap. {_session.CurrentPrimarySectionNumber}/{_session.PrimarySectionCount}"
            : "Sezione supplementare";
        string progress = _session.Progress.Percentage.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        string search = BuildSearchStatus();
        string bookmark = _session.IsCurrentLocationBookmarked ? "   ★" : string.Empty;
        return $"{_session.BookTitle}{author}   {chapter}   Pag. {_session.PageNumber}/{_session.PageCount}   {progress}{search}{bookmark}";
    }

    private string BuildSearchStatus()
    {
        if (_session.SearchQuery is null)
        {
            return string.Empty;
        }

        string truncation = _session.SearchResultsTruncated ? "+" : string.Empty;
        return _session.SearchMatchCount == 0
            ? $"   Cerca «{_session.SearchQuery}»: 0 risultati"
            : $"   Cerca «{_session.SearchQuery}»: {_session.CurrentSearchMatchNumber}/{_session.SearchMatchCount}{truncation}";
    }

    private string BuildNormalFooter() =>
        $"↑/{Binding(ReaderCommand.PreviousLine)} ↓/{Binding(ReaderCommand.NextLine)} riga  PgUp/PgDn pagina  "
        + $"{Binding(ReaderCommand.PreviousChapter)} {Binding(ReaderCommand.NextChapter)} cap.  "
        + $"{Binding(ReaderCommand.Search)} cerca  {Binding(ReaderCommand.NextSearchResult)}/{Binding(ReaderCommand.PreviousSearchResult)} risultato  "
        + $"{Binding(ReaderCommand.ToggleBookmark)} segnalibro  {Binding(ReaderCommand.OpenBookmarks)} elenco  "
        + $"{Binding(ReaderCommand.ToggleToc)} indice  {Binding(ReaderCommand.ToggleMetadata)} metadati  "
        + $"{Binding(ReaderCommand.CycleTheme)} tema  {Binding(ReaderCommand.Quit)}/Esc esci";

    private string BuildTocFooter() =>
        $"↑/{Binding(ReaderCommand.PreviousLine)} ↓/{Binding(ReaderCommand.NextLine)} voce  PgUp/PgDn scorri  Enter apri  "
        + $"{Binding(ReaderCommand.ToggleToc)}/Tab/Esc chiudi  {Binding(ReaderCommand.OpenBookmarks)} segnalibri  "
        + $"{Binding(ReaderCommand.ToggleMetadata)} metadati  {Binding(ReaderCommand.Quit)} esci";

    private string BuildMetadataFooter() =>
        $"↑/{Binding(ReaderCommand.PreviousLine)} ↓/{Binding(ReaderCommand.NextLine)} scorri  PgUp/PgDn pagina  "
        + $"{Binding(ReaderCommand.ToggleMetadata)}/Esc chiudi  {Binding(ReaderCommand.OpenBookmarks)} segnalibri  "
        + $"{Binding(ReaderCommand.ToggleToc)} indice  F1/{Binding(ReaderCommand.Help)} aiuto  {Binding(ReaderCommand.Quit)} esci";

    private string BuildBookmarkFooter() =>
        $"↑/{Binding(ReaderCommand.PreviousLine)} ↓/{Binding(ReaderCommand.NextLine)} voce  PgUp/PgDn scorri  Enter apri  "
        + $"{Binding(ReaderCommand.DeleteBookmark)} elimina  {Binding(ReaderCommand.OpenBookmarks)}/Esc chiudi  "
        + $"{Binding(ReaderCommand.Quit)} esci";

    private string BuildHelp() =>
        $"""
        EReader — comandi M3.3

        ↑ / {Binding(ReaderCommand.PreviousLine)}             riga precedente
        ↓ / {Binding(ReaderCommand.NextLine)}             riga successiva
        PgUp / {Binding(ReaderCommand.PreviousPage)}          pagina precedente
        PgDn / {Binding(ReaderCommand.NextPage)} / Space  pagina successiva
        {Binding(ReaderCommand.PreviousChapter)}                 capitolo precedente
        {Binding(ReaderCommand.NextChapter)}                 capitolo successivo
        {Binding(ReaderCommand.ChapterStart)}                 inizio capitolo
        {Binding(ReaderCommand.ChapterEnd)}                 fine capitolo
        {Binding(ReaderCommand.ToggleToc)} / Tab           apre/chiude indice
        {Binding(ReaderCommand.Search)}                 cerca nel testo logico
        {Binding(ReaderCommand.NextSearchResult)} / {Binding(ReaderCommand.PreviousSearchResult)}             risultato successivo / precedente
        {Binding(ReaderCommand.ToggleBookmark)}                 aggiunge/rimuove bookmark corrente
        {Binding(ReaderCommand.OpenBookmarks)}                 apre/chiude elenco bookmark
        {Binding(ReaderCommand.ToggleMetadata)}                 apre/chiude metadati
        {Binding(ReaderCommand.CycleTheme)}                 cambia tema
        F1 / {Binding(ReaderCommand.Help)}            mostra/nasconde questo aiuto
        {Binding(ReaderCommand.Quit)}                 esci
        Esc               chiude bookmark/metadati/indice/aiuto, altrimenti esce

        I tasti stampabili sopra riflettono config.json; frecce, PgUp/PgDn, Space, Tab, Enter, Esc e F1 restano sempre disponibili.
        Nei bookmark: ↑/↓ o i binding riga selezionano, PgUp/PgDn scorrono, Enter apre, {Binding(ReaderCommand.DeleteBookmark)} elimina.
        Nell'indice: ↑/↓ o i binding riga selezionano, PgUp/PgDn scorrono, Enter apre la voce.
        Nei metadati: ↑/↓ o i binding riga scorrono una riga, PgUp/PgDn una pagina.
        La ricerca opera sul testo logico prima del wrapping; resize e larghezza terminale non cambiano i risultati.
        Il resize ricostruisce il layout mantenendo la stessa ReadingLocation logica.
        Numero pagina e riga possono cambiare dopo il reflow e restano coordinate effimere.
        La percentuale usa il testo logico UTF-16 del Book e resta stabile dopo resize/reflow.
        """;
}
