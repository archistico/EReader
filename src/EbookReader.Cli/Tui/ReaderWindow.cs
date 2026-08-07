using System.Globalization;
using System.Text;
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
    private const string NormalFooter = "↑/k ↓/j riga  PgUp/PgDn pagina  [ ] cap.  / cerca  n/N risultato  b segnalibro  B elenco  t indice  m metadati  q/Esc esci";
    private const string TocFooter = "↑/k ↓/j voce  PgUp/PgDn scorri  Enter apri  t/Tab/Esc chiudi  B segnalibri  m metadati  q esci";
    private const string MetadataFooter = "↑/k ↓/j scorri  PgUp/PgDn pagina  m/Esc chiudi  B segnalibri  t indice  F1/? aiuto  q esci";
    private const string BookmarkFooter = "↑/k ↓/j voce  PgUp/PgDn scorri  Enter apri  d elimina  B/Esc chiudi  q esci";

    private readonly ReaderSession _session;
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

    public ReaderWindow(ReaderSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;

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
        RefreshReader();
    }

    protected override bool OnKeyDown(Key key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_searchInputVisible)
        {
            return HandleSearchInputKey(key);
        }

        if (IsCharacter(key, '/'))
        {
            OpenSearchInput();
            return true;
        }

        if (key == Key.F1 || IsCharacter(key, '?'))
        {
            ToggleHelp();
            return true;
        }

        if (key == Key.Q)
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

        if (key == Key.Tab || IsCharacter(key, 't'))
        {
            ToggleToc();
            return true;
        }

        if (IsCharacter(key, 'm'))
        {
            ToggleMetadata();
            return true;
        }

        if (IsCharacter(key, 'B'))
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

        if (IsCharacter(key, 'b'))
        {
            _session.ToggleBookmark();
            RefreshReader();
            return true;
        }

        if (IsCharacter(key, 'n'))
        {
            Navigate(_session.NextSearchResult);
            return true;
        }

        if (IsCharacter(key, 'N'))
        {
            Navigate(_session.PreviousSearchResult);
            return true;
        }

        if (key == Key.CursorDown || IsCharacter(key, 'j'))
        {
            Navigate(_session.NextLine);
            return true;
        }

        if (key == Key.CursorUp || IsCharacter(key, 'k'))
        {
            Navigate(_session.PreviousLine);
            return true;
        }

        if (key == Key.PageDown || key == Key.L || key == Key.Space)
        {
            Navigate(_session.NextPage);
            return true;
        }

        if (key == Key.PageUp || key == Key.H)
        {
            Navigate(_session.PreviousPage);
            return true;
        }

        if (IsCharacter(key, ']'))
        {
            Navigate(_session.NextChapter);
            return true;
        }

        if (IsCharacter(key, '['))
        {
            Navigate(_session.PreviousChapter);
            return true;
        }

        if (key == Key.G.WithShift)
        {
            Navigate(_session.ChapterEnd);
            return true;
        }

        if (key == Key.G)
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
        if (key == Key.CursorDown || IsCharacter(key, 'j'))
        {
            MoveTocSelection(1);
            return true;
        }

        if (key == Key.CursorUp || IsCharacter(key, 'k'))
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
        if (key == Key.CursorDown || IsCharacter(key, 'j'))
        {
            MoveMetadataScroll(1);
            return true;
        }

        if (key == Key.CursorUp || IsCharacter(key, 'k'))
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
        if (key == Key.CursorDown || IsCharacter(key, 'j'))
        {
            MoveBookmarkSelection(1);
            return true;
        }

        if (key == Key.CursorUp || IsCharacter(key, 'k'))
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

        if (IsCharacter(key, 'd'))
        {
            DeleteSelectedBookmark();
            return true;
        }

        return true;
    }

    private void Navigate(Func<bool> movement)
    {
        if (movement())
        {
            RefreshReader();
        }
    }

    private static bool IsCharacter(Key key, char value) =>
        string.Equals(key.GetPrintableText(), value.ToString(), StringComparison.Ordinal);

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
                ? "F1/?/Esc chiudi aiuto   q esci"
                : _tocVisible
                    ? TocFooter
                    : _metadataVisible
                        ? MetadataFooter
                        : _bookmarksVisible
                            ? BookmarkFooter
                            : NormalFooter;
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

    private static string BuildHelp() =>
        """
        EReader — comandi M2.5

        ↑ / k             riga precedente
        ↓ / j             riga successiva
        PgUp / h          pagina precedente
        PgDn / l / Space  pagina successiva
        [                 capitolo precedente
        ]                 capitolo successivo
        g                 inizio capitolo
        G                 fine capitolo
        t / Tab           apre/chiude indice
        /                 cerca nel testo logico
        n / N             risultato successivo / precedente
        b                 aggiunge/rimuove bookmark corrente
        B                 apre/chiude elenco bookmark
        m                 apre/chiude metadati
        F1 / ?            mostra/nasconde questo aiuto
        q                 esci
        Esc               chiude bookmark/metadati/indice/aiuto, altrimenti esce

        Nei bookmark: ↑/↓ o j/k selezionano, PgUp/PgDn scorrono, Enter apre, d elimina.
        Nell'indice: ↑/↓ o j/k selezionano, PgUp/PgDn scorrono, Enter apre la voce.
        Nei metadati: ↑/↓ o j/k scorrono una riga, PgUp/PgDn una pagina.
        La ricerca opera sul testo logico prima del wrapping; resize e larghezza terminale non cambiano i risultati.
        Il resize ricostruisce il layout mantenendo la stessa ReadingLocation logica.
        Numero pagina e riga possono cambiare dopo il reflow e restano coordinate effimere.
        La percentuale usa il testo logico UTF-16 del Book e resta stabile dopo resize/reflow.
        """;
}
