using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Links;

/// <summary>
/// One actionable hyperlink range in the logical UTF-16 text of a Domain content block.
/// </summary>
public sealed class BookHyperlink
{
    public BookHyperlink(
        ReadingLocation startLocation,
        int textLength,
        string text,
        LinkTarget target,
        HyperlinkRole role = HyperlinkRole.Generic)
    {
        ArgumentNullException.ThrowIfNull(startLocation);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(textLength);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(target);

        if (startLocation.BlockId is null)
        {
            throw new ArgumentException("Un hyperlink logico deve appartenere a un blocco.", nameof(startLocation));
        }

        if (text.Length != textLength)
        {
            throw new ArgumentException("La lunghezza del testo hyperlink deve coincidere con il range UTF-16.", nameof(text));
        }

        StartLocation = startLocation;
        TextLength = textLength;
        Text = text;
        Target = target;
        Role = role;
    }

    public ReadingLocation StartLocation { get; }

    public int TextLength { get; }

    public int EndCharacterOffset => checked(StartLocation.CharacterOffset + TextLength);

    public string Text { get; }

    public LinkTarget Target { get; }

    public HyperlinkRole Role { get; }
}
