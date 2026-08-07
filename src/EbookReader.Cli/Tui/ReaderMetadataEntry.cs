namespace EbookReader.Cli.Tui;

/// <summary>
/// Format-neutral metadata row projected for the reader UI.
/// </summary>
public sealed record ReaderMetadataEntry
{
    public ReaderMetadataEntry(string label, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Label = label.Trim();
        Value = value.Trim();
    }

    public string Label { get; }

    public string Value { get; }
}
