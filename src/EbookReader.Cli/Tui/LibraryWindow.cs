using System.Collections.ObjectModel;
using System.Globalization;
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
    private readonly Label _footer;
    private int _selectedIndex;
    private int _scrollOffset;

    public LibraryWindow(ReadOnlyCollection<ReadingHistoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _entries = entries;
        Title = "EReader — Libreria";
        Width = Dim.Fill();
        Height = Dim.Fill();
        SetScheme(ReaderColorPalette.ChromeScheme);

        Label header = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            Text = $"Libri recenti: {_entries.Count.ToString(CultureInfo.InvariantCulture)}",
        };
        header.SetScheme(ReaderColorPalette.PlainScheme);

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
            Text = "↑/k ↓/j seleziona  PgUp/PgDn pagina  Enter apri  q/Esc chiudi",
        };
        _footer.SetScheme(ReaderColorPalette.PlainScheme);

        _body = new ReaderBodyView { X = 0, Y = 2, Width = Dim.Fill(), Height = Dim.Fill(2) };
        _body.ViewportChanged += (_, _) => RenderList();
        Add(header, top, _body, bottom, _footer);
        RenderList();
    }

    public ReadingHistoryEntry? SelectedEntry { get; private set; }

    protected override bool OnKeyDown(Key key)
    {
        ArgumentNullException.ThrowIfNull(key);

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
        if (key == Key.Enter && _entries.Count > 0)
        {
            ReadingHistoryEntry candidate = _entries[_selectedIndex];
            if (!File.Exists(candidate.BookPath))
            {
                _footer.Text = "File non trovato. Sposta la selezione o premi q/Esc.";
                return true;
            }
            SelectedEntry = candidate;
            App?.RequestStop();
            return true;
        }
        if (key == Key.Esc || IsCharacter(key, 'q'))
        {
            App?.RequestStop();
            return true;
        }
        return base.OnKeyDown(key);
    }

    private void MoveSelection(int delta)
    {
        if (_entries.Count == 0)
        {
            return;
        }
        _selectedIndex = Math.Clamp(_selectedIndex + delta, 0, _entries.Count - 1);
        int height = Math.Max(_body.Viewport.Height, 1);
        if (_selectedIndex < _scrollOffset) _scrollOffset = _selectedIndex;
        if (_selectedIndex >= _scrollOffset + height) _scrollOffset = _selectedIndex - height + 1;
        RenderList();
    }

    private void RenderList()
    {
        int height = Math.Max(_body.Viewport.Height, 1);
        string[] lines = _entries
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

    private static bool IsCharacter(Key key, char expected) =>
        string.Equals(key.GetPrintableText(), expected.ToString(), StringComparison.OrdinalIgnoreCase);
}
