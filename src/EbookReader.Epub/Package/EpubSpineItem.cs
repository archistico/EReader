using System.Collections.ObjectModel;

namespace EbookReader.Epub.Package;

/// <summary>
/// Ordered reference from the OPF spine to one manifest resource.
/// </summary>
public sealed class EpubSpineItem
{
    internal EpubSpineItem(string idRef, bool isLinear, HashSet<string> properties)
    {
        IdRef = idRef;
        IsLinear = isLinear;
        Properties = new ReadOnlyCollection<string>(properties.Order(StringComparer.Ordinal).ToArray());
    }

    public string IdRef { get; }

    public bool IsLinear { get; }

    public IReadOnlyList<string> Properties { get; }
}
