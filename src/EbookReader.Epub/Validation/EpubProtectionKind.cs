namespace EbookReader.Epub.Validation;

/// <summary>
/// Protection mechanism declared for an individual OCF resource.
/// </summary>
public enum EpubProtectionKind
{
    FontObfuscation = 0,
    UnsupportedEncryption = 1,
}
