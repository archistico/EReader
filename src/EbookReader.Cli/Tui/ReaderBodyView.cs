using EbookReader.Layout;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;

namespace EbookReader.Cli.Tui;

/// <summary>
/// Draws reader content with semantic colors while keeping Terminal.Gui out of the layout engine.
/// Overlay screens are rendered as plain white text.
/// </summary>
internal sealed class ReaderBodyView : View
{
    private VisualLine[] _readerLines = [];
    private string[] _plainLines = [];
    private bool _readerMode;

    public ReaderBodyView()
    {
        SetScheme(ReaderColorPalette.PlainScheme);
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
                SetAttribute(ReaderColorPalette.PlainText);
                AddStr(_plainLines[row]);
            }
        }

        return true;
    }

    private void DrawReaderLine(VisualLine line)
    {
        if (line.Kind == VisualLineKind.Heading)
        {
            SetAttribute(ReaderColorPalette.ChapterHeading);
            AddStr(line.Text);
            return;
        }

        int cursor = 0;
        foreach (VisualTextSpan span in line.StyleSpans)
        {
            if (span.StartIndex > cursor)
            {
                SetAttribute(ReaderColorPalette.PlainText);
                AddStr(line.Text[cursor..span.StartIndex]);
            }

            SetAttribute(ReaderColorPalette.ForStyle(span.Style));
            AddStr(line.Text[span.StartIndex..span.EndIndex]);
            cursor = span.EndIndex;
        }

        if (cursor < line.Text.Length)
        {
            SetAttribute(ReaderColorPalette.PlainText);
            AddStr(line.Text[cursor..]);
        }
    }
}
