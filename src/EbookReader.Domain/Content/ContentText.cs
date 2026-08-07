using System.Text;

namespace EbookReader.Domain.Content;

public static class ContentText
{
    public static string GetPlainText(ContentBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);

        return block switch
        {
            ParagraphBlock paragraph => GetPlainText(paragraph.Content),
            HeadingBlock heading => GetPlainText(heading.Content),
            QuoteBlock quote => GetPlainText(quote.Content),
            ListItemBlock item => GetPlainText(item.Content),
            PreformattedBlock preformatted => preformatted.Text,
            ImageBlock image => GetImageText(image),
            ThematicBreakBlock => string.Empty,
            _ => throw new NotSupportedException($"Tipo di blocco non supportato: {block.GetType().FullName}."),
        };
    }

    public static string GetPlainText(IEnumerable<InlineContent> content)
    {
        ArgumentNullException.ThrowIfNull(content);
        StringBuilder builder = new();

        foreach (InlineContent inline in content)
        {
            AppendPlainText(builder, inline);
        }

        return builder.ToString();
    }

    private static string GetImageText(ImageBlock image)
    {
        if (image.AlternativeText is not null && image.Caption is not null)
        {
            return $"{image.AlternativeText}\n{image.Caption}";
        }

        return image.AlternativeText ?? image.Caption ?? string.Empty;
    }

    private static void AppendPlainText(StringBuilder builder, InlineContent inline)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(inline);

        switch (inline)
        {
            case TextRun text:
                builder.Append(text.Text);
                break;
            case LineBreakInline:
                builder.Append('\n');
                break;
            case InlineContainer container:
                foreach (InlineContent child in container.Content)
                {
                    AppendPlainText(builder, child);
                }

                break;
            default:
                throw new NotSupportedException($"Tipo inline non supportato: {inline.GetType().FullName}.");
        }
    }
}
