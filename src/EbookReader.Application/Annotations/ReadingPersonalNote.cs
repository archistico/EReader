using EbookReader.Domain.Reading;

namespace EbookReader.Application.Annotations;

/// <summary>Personal note anchored to one durable logical reading location.</summary>
public sealed record ReadingPersonalNote
{
    public ReadingPersonalNote(ReadingLocation location, string text, DateTimeOffset updatedUtc)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (text.Length > ReadingAnnotationState.MaximumNoteTextLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(text),
                text.Length,
                $"Una nota può contenere al massimo {ReadingAnnotationState.MaximumNoteTextLength} code unit UTF-16.");
        }

        Location = location;
        Text = text;
        UpdatedUtc = updatedUtc.ToUniversalTime();
    }

    public ReadingLocation Location { get; }

    public string Text { get; }

    public DateTimeOffset UpdatedUtc { get; }
}
