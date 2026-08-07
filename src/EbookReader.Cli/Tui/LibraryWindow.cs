using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using EbookReader.Application.Library;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EbookReader.Cli.Tui;

internal sealed class LibraryWindow : Window
{
    private const string HorizontalRule = "────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────";

    private readonly ReadOnlyCollection<ReadingHistoryEntry> _entries;
    private readonly ReaderBodyView _body;
    private readonly Label _header;
    private readonly Label _footer;
    private readonly StringBuilder _filterInput = new();
    private ReadOnlyCollection<ReadingHistoryEntry> _visibleEntries;
    private string _filterQuery = string.Empty;
    private string _filterBeforeEdit = string.Empty;
    private bool _filterInputVisible;
    private int _selectedIndex;
    private int _scrollOffset;

    public LibraryWindow(ReadOnlyCollection<ReadingHistoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _entries = entries;
        _visibleEntries = entries;
        Title = "EReader — Libreria";
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

        Label top = new() { X = 0, Y = 1, Width = Dim.Fill(), Height = 1, Text = HorizontalRule };
        top.SetScheme(ReaderColorPalette.ChromeScheme);
        Label bottom = new() { X = 0, Y = Pos.AnchorEnd(2), Width = Dim.Fill(), Height = 1, Text = HorizontalRule };
        bottom.SetScheme(ReaderColorPalette.ChromeScheme);
        _footer = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
        };
        _footer.SetScheme(ReaderColorPalette.PlainScheme);

        _body = new ReaderBodyView { X = 0, Y = 2, Width = Dim.Fill(), Height = Dim.Fill(2) };
        _body.ViewportChanged += (_, _) => RenderList();
        Add(_header, top, _body, bottom, _footer);
        RefreshLibrary();
    }

    public ReadingHistoryEntry? SelectedEntry { get; private set; }

    protected override bool OnKeyDown(Key key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_filterInputVisible)
        {
            return HandleFilterInputKey(key);
        }

        if (IsCharacter(key, '/'))
        {
            OpenFilterInput();
            return true;
        }
        if (key == Key.CursorDown || IsCharacter(key, 'j'))
        {
            MoveSelection(1);
            return true;
        }
        if (key == Key.CursorUp || IsCharacter(key, 'k'))
        {
            MoveSelection(-1);
            return true;
        }
        if (key == Key.PageDown)
        {
            MoveSelection(Math.Max(_body.Viewport.Height, 1));
            return true;
        }
        if (key == Key.PageUp)
        {
            MoveSelection(-Math.Max(_body.Viewport.Height, 1));
            return true;
        }
        if (key == Key.Enter && _visibleEntries.Count > 0 && _selectedIndex >= 0)
        {
            ReadingHistoryEntry candidate = _visibleEntries[_selectedIndex];
            if (!File.Exists(candidate.BookPath))
            {
                _footer.Text = "File non trovato. Sposta la selezione, / cerca oppure premi q/Esc.";
                return true;
            }
            SelectedEntry = candidate;
            App?.RequestStop();
            return true;
        }
        if (key == Key.Esc)
        {
            if (_filterQuery.Length > 0)
            {
                ApplyFilter(string.Empty, preferredPath: null);
            }
            else
            {
                App?.RequestStop();
            }
            return true;
        }
        if (IsCharacter(key, 'q'))
        {
            App?.RequestStop();
            return true;
        }
        return base.OnKeyDown(key);
    }

    private bool HandleFilterInputKey(Key key)
    {
        if (key == Key.Esc)
        {
            _filterInputVisible = false;
            _filterInput.Clear();
            ApplyFilter(_filterBeforeEdit, preferredPath: CurrentSelectedPath());
            return true;
        }

        if (key == Key.Enter)
        {
            _filterInputVisible = false;
            _filterInput.Clear();
            RefreshLibrary();
            return true;
        }

        if (key == Key.Backspace)
        {
            RemoveLastFilterTextElement();
            ApplyFilter(_filterInput.ToString(), preferredPath: CurrentSelectedPath());
            return true;
        }

        string printable = key.GetPrintableText();
        if (!string.IsNullOrEmpty(printable)
            && printable.All(character => !char.IsControl(character))
            && _filterInput.Length + printable.Length <= ReadingHistorySearch.MaximumQueryLength)
        {
            _filterInput.Append(printable);
            ApplyFilter(_filterInput.ToString(), preferredPath: CurrentSelectedPath());
        }

        return true;
    }

    private void OpenFilterInput()
    {
        _filterBeforeEdit = _filterQuery;
        _filterInput.Clear();
        _filterInput.Append(_filterQuery);
        _filterInputVisible = true;
        RefreshLibrary();
    }

    private void ApplyFilter(string query, string? preferredPath)
    {
        _filterQuery = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();
        _visibleEntries = ReadingHistorySearch.Filter(_entries, _filterQuery);
        _scrollOffset = 0;

        if (_visibleEntries.Count == 0)
        {
            _selectedIndex = -1;
        }
        else
        {
            int preferredIndex = preferredPath is null
                ? -1
                : IndexOfPath(_visibleEntries, preferredPath);
            _selectedIndex = preferredIndex >= 0 ? preferredIndex : 0;
        }

        RefreshLibrary();
    }

    private void MoveSelection(int delta)
    {
        if (_visibleEntries.Count == 0)
        {
            return;
        }
        _selectedIndex = Math.Clamp(_selectedIndex + delta, 0, _visibleEntries.Count - 1);
        int height = Math.Max(_body.Viewport.Height, 1);
        if (_selectedIndex < _scrollOffset)
        {
            _scrollOffset = _selectedIndex;
        }
        if (_selectedIndex >= _scrollOffset + height)
        {
            _scrollOffset = _selectedIndex - height + 1;
        }
        RefreshLibrary();
    }

    private void RefreshLibrary()
    {
        _header.Text = BuildHeaderText();
        _footer.Text = BuildFooterText();
        RenderList();
    }

    private string BuildHeaderText()
    {
        if (_filterQuery.Length == 0)
        {
            return $"Libri recenti: {_entries.Count.ToString(CultureInfo.InvariantCulture)}";
        }

        return $"Libri recenti: {_entries.Count.ToString(CultureInfo.InvariantCulture)}   Filtro «{_filterQuery}»: {_visibleEntries.Count.ToString(CultureInfo.InvariantCulture)}";
    }

    private string BuildFooterText()
    {
        if (_filterInputVisible)
        {
            return $"Cerca libreria: {_filterInput}_   Enter applica   Esc annulla";
        }

        if (_filterQuery.Length > 0)
        {
            return "↑/k ↓/j seleziona  PgUp/PgDn pagina  Enter apri  / modifica filtro  Esc cancella filtro  q chiudi";
        }

        return "↑/k ↓/j seleziona  PgUp/PgDn pagina  Enter apri  / cerca  q/Esc chiudi";
    }

    private void RenderList()
    {
        if (_visibleEntries.Count == 0)
        {
            _body.ShowPlainText("Nessun libro corrisponde al filtro corrente.");
            return;
        }

        int height = Math.Max(_body.Viewport.Height, 1);
        string[] lines = _visibleEntries
            .Skip(_scrollOffset)
            .Take(height)
            .Select((entry, index) => FormatEntry(entry, _scrollOffset + index))
            .ToArray();
        _body.ShowPlainText(string.Join(Environment.NewLine, lines));
    }

    private string FormatEntry(ReadingHistoryEntry entry, int index)
    {
        string marker = index == _selectedIndex ? ">" : " ";
        string missing = File.Exists(entry.BookPath) ? string.Empty : " [mancante]";
        string author = string.IsNullOrWhiteSpace(entry.AuthorLine) ? string.Empty : $" — {entry.AuthorLine}";
        return $"{marker} {entry.Title}{author} — {entry.LastOpenedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)}{missing}";
    }

    private string? CurrentSelectedPath() =>
        _selectedIndex >= 0 && _selectedIndex < _visibleEntries.Count
            ? _visibleEntries[_selectedIndex].BookPath
            : null;

    private static int IndexOfPath(ReadOnlyCollection<ReadingHistoryEntry> entries, string path)
    {
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        for (int index = 0; index < entries.Count; index++)
        {
            if (string.Equals(entries[index].BookPath, path, comparison))
            {
                return index;
            }
        }
        return -1;
    }

    private void RemoveLastFilterTextElement()
    {
        if (_filterInput.Length == 0)
        {
            return;
        }

        string current = _filterInput.ToString();
        int[] starts = StringInfo.ParseCombiningCharacters(current);
        _filterInput.Length = starts[^1];
    }

    private static bool IsCharacter(Key key, char expected) =>
        string.Equals(key.GetPrintableText(), expected.ToString(), StringComparison.OrdinalIgnoreCase);
}
