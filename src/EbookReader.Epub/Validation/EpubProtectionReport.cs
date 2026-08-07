using System.Collections.ObjectModel;

namespace EbookReader.Epub.Validation;

/// <summary>
/// Parsed protection metadata. This is inspection only: EReader never decrypts protected content.
/// </summary>
public sealed class EpubProtectionReport
{
    internal EpubProtectionReport(
        bool hasEncryptionDocument,
        bool hasRightsManagementDocument,
        List<EpubProtectedResource> resources)
    {
        HasEncryptionDocument = hasEncryptionDocument;
        HasRightsManagementDocument = hasRightsManagementDocument;
        Resources = new ReadOnlyCollection<EpubProtectedResource>(resources);
    }

    public bool HasEncryptionDocument { get; }

    public bool HasRightsManagementDocument { get; }

    public IReadOnlyList<EpubProtectedResource> Resources { get; }

    public bool HasFontObfuscation => Resources.Any(resource => resource.Kind == EpubProtectionKind.FontObfuscation);

    public bool HasUnsupportedEncryption =>
        Resources.Any(resource => resource.Kind == EpubProtectionKind.UnsupportedEncryption);
}
