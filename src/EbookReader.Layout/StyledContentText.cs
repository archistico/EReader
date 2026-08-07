using System.Text;
using EbookReader.Domain.Content;

namespace EbookReader.Layout;

internal static class StyledContentText
{
    public static StyledLogicalText FromBlock(ContentBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);

        return block switch
        {
            ParagraphBlock paragraph => FromInline(paragraph.Content),
            HeadingBlock heading => FromInline(heading.Content),
            QuoteBlock quote => FromInline(quote.Content),
            ListItemBlock item => FromInline(item.Content),
            _ => Plain(ContentText.GetPlainText(block)),
        };
    }

    private static StyledLogicalText FromInline(IReadOnlyList<InlineContent> content)
    {
        StringBuilder text = new();
        List<VisualTextStyle> styles = [];

        foreach (InlineContent inline in content)
        {
            Append(text, styles, inline, VisualTextStyle.None);
        }

        return new StyledLogicalText(text.ToString(), styles.ToArray());
    }

    private static StyledLogicalText Plain(string text) =>
        new(text, new VisualTextStyle[text.Length]);

    private static void Append(
        StringBuilder text,
        List<VisualTextStyle> styles,
        InlineContent inline,
        VisualTextStyle inheritedStyle)
    {
        switch (inline)
        {
            case TextRun run:
                text.Append(run.Text);
                AppendStyles(styles, run.Text.Length, inheritedStyle);
                break;

            case LineBreakInline:
                text.Append('\n');
                styles.Add(inheritedStyle);
                break;

            case StrongSpan strong:
                AppendChildren(text, styles, strong.Content, inheritedStyle | VisualTextStyle.Strong);
                break;

            case EmphasisSpan emphasis:
                AppendChildren(text, styles, emphasis.Content, inheritedStyle | VisualTextStyle.Emphasis);
                break;

            case InlineContainer container:
                AppendChildren(text, styles, container.Content, inheritedStyle);
                break;

            default:
                throw new NotSupportedException($"Tipo inline non supportato dal layout: {inline.GetType().FullName}.");
        }
    }

    private static void AppendChildren(
        StringBuilder text,
        List<VisualTextStyle> styles,
        IReadOnlyList<InlineContent> children,
        VisualTextStyle inheritedStyle)
    {
        foreach (InlineContent child in children)
        {
            Append(text, styles, child, inheritedStyle);
        }
    }

    private static void AppendStyles(List<VisualTextStyle> styles, int count, VisualTextStyle style)
    {
        for (int index = 0; index < count; index++)
        {
            styles.Add(style);
        }
    }
}

internal sealed record StyledLogicalText(string Text, VisualTextStyle[] Styles);
