using System.Globalization;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;

namespace EbookReader.Cli.Reading;

/// <summary>
/// M1.0 deterministic, non-paginated console projection of the format-neutral Domain book.
/// It deliberately performs no terminal-width wrapping; pagination belongs to M1.1+.
/// </summary>
internal static class BookConsoleRenderer
{
    public static void Write(Book book, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(output);

        WriteMetadata(book.Metadata, output);

        foreach (ReadingSection section in book.ReadingOrder)
        {
            foreach (ContentBlock block in section.Blocks)
            {
                WriteBlock(block, output);
            }
        }
    }

    private static void WriteMetadata(BookMetadata metadata, TextWriter output)
    {
        output.WriteLine(metadata.Title);

        if (metadata.Subtitle is not null)
        {
            output.WriteLine(metadata.Subtitle);
        }

        string[] authors = metadata.Contributors
            .Where(contributor => contributor.Role == ContributorRole.Author)
            .Select(contributor => contributor.Name)
            .ToArray();

        if (authors.Length > 0)
        {
            output.Write("di ");
            output.WriteLine(string.Join(", ", authors));
        }

        output.WriteLine();
    }

    private static void WriteBlock(ContentBlock block, TextWriter output)
    {
        switch (block)
        {
            case HeadingBlock heading:
                WriteTextBlock(ContentText.GetPlainText(heading), output);
                break;

            case ParagraphBlock paragraph:
                WriteTextBlock(ContentText.GetPlainText(paragraph), output);
                break;

            case QuoteBlock quote:
                WriteQuote(ContentText.GetPlainText(quote), quote.Depth, output);
                break;

            case ListItemBlock item:
                WriteListItem(item, output);
                break;

            case PreformattedBlock preformatted:
                WritePreformatted(preformatted.Text, output);
                break;

            case ImageBlock image:
                WriteImage(image, output);
                break;

            case ThematicBreakBlock:
                output.WriteLine("---");
                output.WriteLine();
                break;

            default:
                throw new NotSupportedException(
                    $"Tipo di blocco Domain non supportato dal renderer M1.0: {block.GetType().FullName}.");
        }
    }

    private static void WriteTextBlock(string text, TextWriter output)
    {
        if (text.Length > 0)
        {
            output.WriteLine(text);
        }

        output.WriteLine();
    }

    private static void WriteQuote(string text, int depth, TextWriter output)
    {
        string prefix = string.Concat(Enumerable.Repeat("> ", depth));
        WritePrefixedLines(text, prefix, prefix, output);
        output.WriteLine();
    }

    private static void WriteListItem(ListItemBlock item, TextWriter output)
    {
        string indentation = new(' ', checked((item.Depth - 1) * 2));
        string marker = item.ListKind == ListKind.Ordered
            ? item.Ordinal is int ordinal
                ? $"{ordinal.ToString(CultureInfo.InvariantCulture)}. "
                : "#. "
            : "- ";
        string continuation = new(' ', marker.Length);
        string text = ContentText.GetPlainText(item);

        WritePrefixedLines(
            text,
            indentation + marker,
            indentation + continuation,
            output);
    }

    private static void WritePreformatted(string text, TextWriter output)
    {
        output.Write(text);
        if (!EndsWithNewLine(text))
        {
            output.WriteLine();
        }

        output.WriteLine();
    }

    private static void WriteImage(ImageBlock image, TextWriter output)
    {
        string description = image.AlternativeText switch
        {
            string alternativeText when image.Caption is string caption => $"{alternativeText} — {caption}",
            string alternativeText => alternativeText,
            null when image.Caption is string caption => caption,
            _ => string.Empty,
        };

        output.WriteLine(description.Length == 0
            ? "[Immagine]"
            : $"[Immagine: {description}]");
        output.WriteLine();
    }

    private static void WritePrefixedLines(
        string text,
        string firstPrefix,
        string continuationPrefix,
        TextWriter output)
    {
        string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        string[] lines = normalized.Split('\n');

        if (lines.Length == 0)
        {
            output.WriteLine(firstPrefix);
            return;
        }

        output.Write(firstPrefix);
        output.WriteLine(lines[0]);

        for (int index = 1; index < lines.Length; index++)
        {
            output.Write(continuationPrefix);
            output.WriteLine(lines[index]);
        }
    }

    private static bool EndsWithNewLine(string text) =>
        text.EndsWith('\n') || text.EndsWith('\r');
}
