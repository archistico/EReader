namespace EbookReader.Application.Progress;

/// <summary>
/// Stable logical progress through a book. Units are UTF-16 code units from the Domain plain-text projection,
/// never layout pages, visual lines or terminal cells.
/// </summary>
public sealed record BookProgress
{
    public BookProgress(long consumedUnits, long totalUnits)
    {
        if (consumedUnits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(consumedUnits), consumedUnits, "Le unità consumate non possono essere negative.");
        }

        if (totalUnits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalUnits), totalUnits, "Le unità totali non possono essere negative.");
        }

        if (consumedUnits > totalUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(consumedUnits), consumedUnits, "Le unità consumate non possono superare il totale.");
        }

        ConsumedUnits = consumedUnits;
        TotalUnits = totalUnits;
    }

    public long ConsumedUnits { get; }

    public long TotalUnits { get; }

    public decimal Fraction => TotalUnits == 0
        ? 0m
        : (decimal)ConsumedUnits / TotalUnits;

    public decimal Percentage => Fraction * 100m;

    public bool IsComplete => TotalUnits > 0 && ConsumedUnits == TotalUnits;
}
