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
    private const string NormalFooter = "↑/k ↓/j riga  PgUp/h PgDn/l/Space pagina  [ ] capitolo  t/Tab indice  m metadati  F1/? aiuto  q/Esc esci";
    private const string TocFooter = "↑/k ↓/j voce  PgUp/PgDn scorri  Enter apri  t/Tab/Esc chiudi  m metadati  q esci";
    private const string MetadataFooter = "↑/k ↓/j scorri  PgUp/PgDn pagina  m/Esc chiudi  t indice  F1/? aiuto  q esci";

    private readonly ReaderSession _session;
    private static readonly string HorizontalRule = new('─', 1024);

    private readonly Label _header;
    private readonly Label _headerSeparator;
    private readonly Label _body;
    private readonly Label _footerSeparator;
    private readonly Label _footer;
    private bool _helpVisible;
    private bool _tocVisible;
    private bool _metadataVisible;
    private int _tocSelectedIndex = -1;
    private int _tocScrollOffset;
    private int _metadataScrollOffset;
    private bool _synchronizingViewport;

    public ReaderWindow(ReaderSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;

        Title = "EReader";
        Width = Dim.Fill();
        Height = Dim.Fill();

        _header = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
        };

        _headerSeparator = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = 1,
            Text = HorizontalRule,
        };

        _body = new Label
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

        _footer = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
        };

        _body.ViewportChanged += (_, _) => SynchronizeViewport();

        Add(_header, _headerSeparator, _body, _footerSeparator, _footer);
        RefreshReader();
    }

    protected override bool OnKeyDown(Key key)
    {
        ArgumentNullException.ThrowIfNull(key);

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
            if (_metadataVisible)
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

    private void Navigate(Func<bool> movement)
    {
        if (movement())
        {
            RefreshReader();
        }
    }

    private static bool IsCharacter(Key key, char value) =>
        string.Equals(key.GetPrintableText(), value.ToString(), StringComparison.Ordinal);

    private void ToggleHelp()
    {
        _helpVisible = !_helpVisible;
        if (_helpVisible)
        {
            _tocVisible = false;
            _metadataVisible = false;
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
        _metadataVisible = true;
        _metadataScrollOffset = 0;
        RefreshReader();
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
        _body.Text = _helpVisible
            ? BuildHelp()
            : _tocVisible
                ? BuildToc()
                : _metadataVisible
                    ? BuildMetadata()
                    : _session.RenderCurrentViewport();
        _footer.Text = _helpVisible
            ? "F1/?/Esc chiudi aiuto   q esci"
            : _tocVisible
                ? TocFooter
                : _metadataVisible
                    ? MetadataFooter
                    : NormalFooter;
    }

    private string BuildHeader()
    {
        string author = string.IsNullOrWhiteSpace(_session.AuthorLine) ? string.Empty : $" — {_session.AuthorLine}";
        if (_metadataVisible)
        {
            return $"{_session.BookTitle}{author}   Metadati   {_session.MetadataEntries.Count} campi";
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
        return $"{_session.BookTitle}{author}   {chapter}   Pag. {_session.PageNumber}/{_session.PageCount}";
    }

    private static string BuildHelp() =>
        """
        EReader — comandi M2.2

        ↑ / k             riga precedente
        ↓ / j             riga successiva
        PgUp / h          pagina precedente
        PgDn / l / Space  pagina successiva
        [                 capitolo precedente
        ]                 capitolo successivo
        g                 inizio capitolo
        G                 fine capitolo
        t / Tab           apre/chiude indice
        m                 apre/chiude metadati
        F1 / ?            mostra/nasconde questo aiuto
        q                 esci
        Esc               chiude metadati/indice/aiuto, altrimenti esce

        Nell'indice: ↑/↓ o j/k selezionano, PgUp/PgDn scorrono, Enter apre la voce.
        Nei metadati: ↑/↓ o j/k scorrono una riga, PgUp/PgDn una pagina.
        Il resize ricostruisce il layout mantenendo la stessa ReadingLocation logica.
        Numero pagina e riga possono cambiare dopo il reflow e restano coordinate effimere.
        """;
}
