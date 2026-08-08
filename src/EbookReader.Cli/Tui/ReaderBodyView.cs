using EbookReader.Application.Annotations;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;
using EbookReader.Layout;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;

namespace EbookReader.Cli.Tui;

/// <summary>
/// Draws reader content with semantic colors while keeping Terminal.Gui out of the layout engine.
/// Overlay screens are rendered with the active theme plain-text attribute.
/// </summary>
internal sealed class ReaderBodyView : View
{
    private VisualLine[] _readerLines = [];
    private string[] _plainLines = [];
    private bool _readerMode;
    private ReadingHighlightRange[] _highlights = [];
    private ReaderTheme _theme = ReaderThemeCatalog.Default;

    public ReaderBodyView()
    {
        SetScheme(ReaderColorPalette.PlainScheme);
    }

    public void ApplyTheme(ReaderTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        _theme = theme;
        SetScheme(theme.PlainScheme);
        SetNeedsDraw();
    }

    public void SetHighlights(IReadOnlyList<ReadingHighlightRange> highlights)
    {
        ArgumentNullException.ThrowIfNull(highlights);
        _highlights = highlights.ToArray();
        SetNeedsDraw();
    }

    public void ShowReaderLines(VisualLine[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _readerLines = (VisualLine[])lines.Clone();
        _plainLines = [];
        _readerMode = true;
        SetNeedsDraw();
    }

    public void ShowPlainText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _plainLines = text.ReplaceLineEndings("\n").Split('\n');
        _readerLines = [];
        _readerMode = false;
        SetNeedsDraw();
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        _ = context;
        int height = Math.Max(Viewport.Height, 0);
        int lineCount = _readerMode ? _readerLines.Length : _plainLines.Length;
        int visibleLines = Math.Min(height, lineCount);

        for (int row = 0; row < visibleLines; row++)
        {
            Move(0, row);
            if (_readerMode)
            {
                DrawReaderLine(_readerLines[row]);
            }
            else
            {
                SetAttribute(_theme.PlainText);
                AddStr(_plainLines[row]);
            }
        }

        return true;
    }

    private void DrawReaderLine(VisualLine line)
    {
        if (IsHighlighted(line))
        {
            SetAttribute(_theme.HighlightText);
            AddStr(line.Text);
            return;
        }

        if (line.Kind == VisualLineKind.Heading)
        {
            SetAttribute(_theme.ChapterHeading);
            AddStr(line.Text);
            return;
        }

        int cursor = 0;
        foreach (VisualTextSpan span in line.StyleSpans)
        {
            if (span.StartIndex > cursor)
            {
                SetAttribute(_theme.PlainText);
                AddStr(line.Text[cursor..span.StartIndex]);
            }

            SetAttribute(_theme.ForStyle(span.Style));
            AddStr(line.Text[span.StartIndex..span.EndIndex]);
            cursor = span.EndIndex;
        }

        if (cursor < line.Text.Length)
        {
            SetAttribute(_theme.PlainText);
            AddStr(line.Text[cursor..]);
        }
    }

    private bool IsHighlighted(VisualLine line)
    {
        if (line.SectionId is not SectionId sectionId
            || line.BlockId is not BlockId blockId
            || line.SourceStartOffset is not int start
            || line.SourceEndOffset is not int end
            || end <= start)
        {
            return false;
        }

        ReadingLocation lineStart = new(sectionId, blockId, start);
        ReadingLocation lineEnd = new(sectionId, blockId, end);
        return _highlights.Any(highlight => highlight.Intersects(lineStart, lineEnd));
    }
}
