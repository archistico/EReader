using System.Xml;
using System.Xml.Linq;
using EbookReader.Epub.Container;
using EbookReader.Epub.Package;

namespace EbookReader.Epub.Validation;

/// <summary>
/// Parses OCF protection metadata without attempting decryption or DRM circumvention.
/// </summary>
public static class EpubProtectionInspector
{
    public const string EncryptionDocumentPath = "META-INF/encryption.xml";
    public const string RightsManagementDocumentPath = "META-INF/rights.xml";
    public const string IdpfFontObfuscationAlgorithm = "http://www.idpf.org/2008/embedding";
    public const string AdobeFontObfuscationAlgorithm = "http://ns.adobe.com/pdf/enc#RC";

    private const string ContainerNamespaceValue = "urn:oasis:names:tc:opendocument:xmlns:container";
    private const string XmlEncryptionNamespaceValue = "http://www.w3.org/2001/04/xmlenc#";

    private static readonly XNamespace ContainerNamespace = ContainerNamespaceValue;
    private static readonly XNamespace XmlEncryptionNamespace = XmlEncryptionNamespaceValue;

    private static readonly HashSet<string> ForbiddenProtectedPaths = new(StringComparer.Ordinal)
    {
        "mimetype",
        "META-INF/container.xml",
        EncryptionDocumentPath,
        "META-INF/manifest.xml",
        "META-INF/metadata.xml",
        RightsManagementDocumentPath,
        "META-INF/signatures.xml",
    };

    public static EpubProtectionReport Inspect(EpubContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        OcfPath encryptionPath = OcfPath.FromArchiveEntry(EncryptionDocumentPath);
        bool hasRightsManagementDocument =
            container.Contains(OcfPath.FromArchiveEntry(RightsManagementDocumentPath));

        if (!container.Contains(encryptionPath))
        {
            return new EpubProtectionReport(
                hasEncryptionDocument: false,
                hasRightsManagementDocument,
                []);
        }

        using Stream stream = container.OpenEntry(encryptionPath);
        XDocument document = LoadEncryptionXml(stream);
        XElement root = ValidateRoot(document);

        XElement[] encryptedDataElements = root
            .Descendants(XmlEncryptionNamespace + "EncryptedData")
            .ToArray();

        if (encryptedDataElements.Length == 0)
        {
            throw Error(
                EpubProtectionErrorCode.MissingEncryptedData,
                "META-INF/encryption.xml non dichiara alcuna risorsa EncryptedData.");
        }

        if (encryptedDataElements.Length > EpubProtectionLimits.MaxProtectedResources)
        {
            throw Error(
                EpubProtectionErrorCode.InvalidProtectionDocument,
                $"META-INF/encryption.xml supera il limite di {EpubProtectionLimits.MaxProtectedResources} risorse protette.");
        }

        HashSet<string> seenPaths = new(StringComparer.Ordinal);
        List<EpubProtectedResource> resources = new(encryptedDataElements.Length);

        foreach (XElement encryptedData in encryptedDataElements)
        {
            EpubProtectedResource resource = ReadProtectedResource(container, encryptedData);
            if (!seenPaths.Add(resource.Path.Value))
            {
                throw Error(
                    EpubProtectionErrorCode.DuplicateProtectedResource,
                    $"La risorsa '{resource.Path.Value}' è dichiarata più volte in encryption.xml.");
            }

            resources.Add(resource);
        }

        return new EpubProtectionReport(
            hasEncryptionDocument: true,
            hasRightsManagementDocument,
            resources);
    }

    public static void ValidateAgainstPackage(
        EpubProtectionReport report,
        EpubPackageDocument package)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(package);

        Dictionary<string, EpubManifestItem> manifestByPath = package.Manifest
            .Where(item => item.LocalPath is not null)
            .ToDictionary(item => item.LocalPath!.Value, StringComparer.Ordinal);

        foreach (EpubProtectedResource resource in report.Resources)
        {
            if (resource.Kind != EpubProtectionKind.FontObfuscation)
            {
                continue;
            }

            if (!manifestByPath.TryGetValue(resource.Path.Value, out EpubManifestItem? item))
            {
                throw Error(
                    EpubProtectionErrorCode.FontObfuscationResourceNotInManifest,
                    $"Il font offuscato '{resource.Path.Value}' non è dichiarato nel manifest OPF.");
            }

            if (!IsFontMediaType(item.MediaType))
            {
                throw Error(
                    EpubProtectionErrorCode.FontObfuscationTargetNotFont,
                    $"La risorsa offuscata '{resource.Path.Value}' ha media-type '{item.MediaType}', non un media type font supportato.");
            }
        }
    }

    private static EpubProtectedResource ReadProtectedResource(
        EpubContainer container,
        XElement encryptedData)
    {
        string? algorithm = encryptedData
            .Element(XmlEncryptionNamespace + "EncryptionMethod")
            ?.Attribute("Algorithm")
            ?.Value.Trim();

        XElement? cipherReference = encryptedData
            .Element(XmlEncryptionNamespace + "CipherData")
            ?.Element(XmlEncryptionNamespace + "CipherReference");

        string? uri = cipherReference?.Attribute("URI")?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw Error(
                EpubProtectionErrorCode.InvalidCipherReference,
                "EncryptedData deve contenere CipherData/CipherReference con attributo URI.");
        }

        OcfPath path;
        try
        {
            // URLs in META-INF documents resolve against the OCF container root.
            path = OcfPath.FromContainerReference(uri);
        }
        catch (EpubContainerException exception)
        {
            throw new EpubProtectionException(
                EpubProtectionErrorCode.InvalidCipherReference,
                $"CipherReference URI non valido: '{uri}'.",
                exception);
        }

        if (ForbiddenProtectedPaths.Contains(path.Value) ||
            container.RootFiles.Any(rootFile => string.Equals(rootFile.Path.Value, path.Value, StringComparison.Ordinal)))
        {
            throw Error(
                EpubProtectionErrorCode.ForbiddenProtectedResource,
                $"La risorsa OCF '{path.Value}' non può essere cifrata o offuscata.");
        }

        if (!container.Contains(path))
        {
            throw Error(
                EpubProtectionErrorCode.ProtectedResourceNotFound,
                $"La risorsa protetta '{path.Value}' non esiste nel contenitore EPUB.");
        }

        EpubProtectionKind kind = IsKnownFontObfuscationAlgorithm(algorithm)
            ? EpubProtectionKind.FontObfuscation
            : EpubProtectionKind.UnsupportedEncryption;

        return new EpubProtectedResource(path, algorithm, kind);
    }

    private static XDocument LoadEncryptionXml(Stream stream)
    {
        using MemoryStream bounded = new();
        byte[] buffer = new byte[16 * 1024];
        int total = 0;

        while (true)
        {
            int read = stream.Read(buffer.AsSpan());
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > EpubProtectionLimits.MaxEncryptionDocumentBytes)
            {
                throw Error(
                    EpubProtectionErrorCode.ProtectionDocumentTooLarge,
                    $"META-INF/encryption.xml supera il limite di {EpubProtectionLimits.MaxEncryptionDocumentBytes} byte.");
            }

            bounded.Write(buffer.AsSpan(0, read));
        }

        bounded.Position = 0;
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = EpubProtectionLimits.MaxEncryptionDocumentBytes,
        };

        try
        {
            using XmlReader reader = XmlReader.Create(bounded, settings);
            return XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            throw new EpubProtectionException(
                EpubProtectionErrorCode.InvalidProtectionXml,
                "META-INF/encryption.xml non è XML valido o contiene costrutti XML non consentiti.",
                exception);
        }
    }

    private static XElement ValidateRoot(XDocument document)
    {
        XElement? root = document.Root;
        if (root is null || !string.Equals(root.Name.LocalName, "encryption", StringComparison.Ordinal))
        {
            throw Error(
                EpubProtectionErrorCode.InvalidProtectionXml,
                "Root element 'encryption' mancante in META-INF/encryption.xml.");
        }

        if (root.Name.Namespace != ContainerNamespace)
        {
            throw Error(
                EpubProtectionErrorCode.InvalidProtectionNamespace,
                $"Namespace encryption.xml non valido: '{root.Name.NamespaceName}'.");
        }

        return root;
    }

    private static bool IsKnownFontObfuscationAlgorithm(string? algorithm) =>
        string.Equals(algorithm, IdpfFontObfuscationAlgorithm, StringComparison.Ordinal) ||
        string.Equals(algorithm, AdobeFontObfuscationAlgorithm, StringComparison.Ordinal);

    private static bool IsFontMediaType(string mediaType) =>
        mediaType.StartsWith("font/", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(mediaType, "application/vnd.ms-opentype", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(mediaType, "application/font-sfnt", StringComparison.OrdinalIgnoreCase);

    private static EpubProtectionException Error(
        EpubProtectionErrorCode errorCode,
        string message) =>
        new(errorCode, message);
}
