namespace EbookReader.Domain.Content;

public sealed class TextRun : InlineContent
{
    public TextRun(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            throw new ArgumentException("Il testo non può essere vuoto.", nameof(text));
        }

        Text = text;
    }

    public string Text { get; }
}
