using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EbookReader.Epub.Container;

namespace EbookReader.Epub.Package;

/// <summary>
/// Safe, bounded reader for EPUB 2/3 OPF Package Documents.
/// </summary>
public static class EpubPackageReader
{
    private const string OpfNamespaceValue = "http://www.idpf.org/2007/opf";
    private const string DcNamespaceValue = "http://purl.org/dc/elements/1.1/";
    private const string XmlNamespaceValue = "http://www.w3.org/XML/1998/namespace";

    private static readonly XNamespace OpfNamespace = OpfNamespaceValue;
    private static readonly XNamespace DcNamespace = DcNamespaceValue;
    private static readonly XNamespace XmlNamespace = XmlNamespaceValue;

    public static EpubPackageDocument Read(EpubContainer container) =>
        ReadCore(container, requireLocalManifestResources: true);

    internal static EpubPackageDocument ReadForRecovery(EpubContainer container) =>
        ReadCore(container, requireLocalManifestResources: false);

    private static EpubPackageDocument ReadCore(
        EpubContainer container,
        bool requireLocalManifestResources)
    {
        ArgumentNullException.ThrowIfNull(container);

        OcfPath packagePath = container.DefaultRootFile.Path;
        using Stream stream = container.OpenDefaultPackageDocument();
        XDocument document = LoadXml(stream);
        XElement package = ValidatePackageRoot(document);
        string version = RequiredAttribute(package, "version", EpubPackageErrorCode.UnsupportedPackageVersion);
        if (version is not ("2.0" or "3.0"))
        {
            throw Error(
                EpubPackageErrorCode.UnsupportedPackageVersion,
                $"Versione OPF non supportata: '{version}'. Sono supportati EPUB 2.0 ed EPUB 3.x con package version='3.0'.");
        }

        string uniqueIdentifierId = RequiredAttribute(
            package,
            "unique-identifier",
            EpubPackageErrorCode.MissingUniqueIdentifierAttribute);

        EpubPackageMetadata metadata = ReadMetadata(package, version, uniqueIdentifierId);
        List<EpubManifestItem> manifest = ReadManifest(
            container,
            package,
            packagePath,
            requireLocalManifestResources);
        ValidateManifestReferences(manifest);
        (List<EpubSpineItem> spine, string? tocId, string? pageProgressionDirection) =
            ReadSpine(package, manifest);

        return new EpubPackageDocument(
            packagePath,
            version,
            uniqueIdentifierId,
            metadata,
            manifest,
            spine,
            tocId,
            pageProgressionDirection);
    }

    private static XDocument LoadXml(Stream stream)
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
            if (total > EpubPackageLimits.MaxPackageDocumentBytes)
            {
                throw Error(
                    EpubPackageErrorCode.PackageDocumentTooLarge,
                    $"Il Package Document supera il limite di {EpubPackageLimits.MaxPackageDocumentBytes} byte.");
            }

            bounded.Write(buffer.AsSpan(0, read));
        }

        bounded.Position = 0;
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = EpubPackageLimits.MaxPackageDocumentBytes,
        };

        try
        {
            using XmlReader reader = XmlReader.Create(bounded, settings);
            return XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            throw new EpubPackageException(
                EpubPackageErrorCode.InvalidPackageXml,
                "Il Package Document OPF non è XML valido o contiene costrutti XML non consentiti.",
                exception);
        }
    }

    private static XElement ValidatePackageRoot(XDocument document)
    {
        XElement? root = document.Root;
        if (root is null || !string.Equals(root.Name.LocalName, "package", StringComparison.Ordinal))
        {
            throw Error(EpubPackageErrorCode.InvalidPackageXml, "Root element OPF 'package' mancante.");
        }

        if (root.Name.Namespace != OpfNamespace)
        {
            throw Error(
                EpubPackageErrorCode.InvalidPackageNamespace,
                $"Namespace OPF non valido: '{root.Name.NamespaceName}'.");
        }

        return root;
    }

    private static EpubPackageMetadata ReadMetadata(
        XElement package,
        string version,
        string uniqueIdentifierId)
    {
        XElement metadataElement = SingleChild(package, "metadata", EpubPackageErrorCode.MissingMetadata);
        XElement[] dcElements = metadataElement.Elements()
            .Where(element => element.Name.Namespace == DcNamespace)
            .ToArray();

        if (dcElements.Length > EpubPackageLimits.MaxMetadataEntries)
        {
            throw Error(EpubPackageErrorCode.InvalidPackage, "Il Package Document contiene troppi metadata.");
        }

        List<EpubDublinCoreMetadata> values = new(dcElements.Length);
        foreach (XElement element in dcElements)
        {
            string value = CollapseWhitespace(element.Value);
            if (value.Length == 0)
            {
                continue;
            }

            values.Add(new EpubDublinCoreMetadata(
                element.Name.LocalName,
                value,
                OptionalAttribute(element, "id"),
                OptionalAttribute(element, XmlNamespace + "lang"),
                OptionalAttribute(element, OpfNamespace + "role"),
                OptionalAttribute(element, OpfNamespace + "file-as"),
                OptionalAttribute(element, OpfNamespace + "scheme")));
        }

        EpubDublinCoreMetadata[] identifiers = values
            .Where(value => string.Equals(value.Name, "identifier", StringComparison.Ordinal))
            .ToArray();
        if (identifiers.Length == 0)
        {
            throw Error(EpubPackageErrorCode.MissingIdentifier, "Metadata dc:identifier mancante.");
        }

        EpubDublinCoreMetadata[] uniqueIdentifiers = identifiers
            .Where(value => string.Equals(value.Id, uniqueIdentifierId, StringComparison.Ordinal))
            .ToArray();
        if (uniqueIdentifiers.Length != 1)
        {
            throw Error(
                EpubPackageErrorCode.UniqueIdentifierNotFound,
                $"unique-identifier='{uniqueIdentifierId}' deve identificare esattamente un dc:identifier.");
        }

        EpubDublinCoreMetadata uniqueIdentifier = uniqueIdentifiers[0];

        RequireDc(values, "title", EpubPackageErrorCode.MissingTitle);
        RequireDc(values, "language", EpubPackageErrorCode.MissingLanguage);

        XElement[] publicationModified = metadataElement
            .Elements(OpfNamespace + "meta")
            .Where(element =>
                string.Equals(OptionalAttribute(element, "property"), "dcterms:modified", StringComparison.Ordinal) &&
                OptionalAttribute(element, "refines") is null)
            .ToArray();

        string? modified = null;
        if (string.Equals(version, "3.0", StringComparison.Ordinal))
        {
            if (publicationModified.Length == 0)
            {
                throw Error(
                    EpubPackageErrorCode.MissingModifiedMetadata,
                    "Un Package Document EPUB 3 deve dichiarare meta property='dcterms:modified'.");
            }

            if (publicationModified.Length != 1)
            {
                throw Error(
                    EpubPackageErrorCode.InvalidModifiedMetadata,
                    "Un Package Document EPUB 3 deve contenere esattamente un dcterms:modified della pubblicazione.");
            }

            modified = CollapseWhitespace(publicationModified[0].Value);
            if (!IsValidModifiedTimestamp(modified))
            {
                throw Error(
                    EpubPackageErrorCode.InvalidModifiedMetadata,
                    $"dcterms:modified non valido: '{modified}'. Atteso YYYY-MM-DDThh:mm:ssZ in UTC.");
            }
        }

        return new EpubPackageMetadata(values, uniqueIdentifier.Value, modified);
    }

    private static List<EpubManifestItem> ReadManifest(
        EpubContainer container,
        XElement package,
        OcfPath packagePath,
        bool requireLocalManifestResources)
    {
        XElement manifestElement = SingleChild(package, "manifest", EpubPackageErrorCode.MissingManifest);
        XElement[] itemElements = manifestElement.Elements(OpfNamespace + "item").ToArray();
        if (itemElements.Length == 0)
        {
            throw Error(EpubPackageErrorCode.EmptyManifest, "Il manifest OPF non contiene item.");
        }

        if (itemElements.Length > EpubPackageLimits.MaxManifestItems)
        {
            throw Error(EpubPackageErrorCode.InvalidPackage, "Il manifest OPF contiene troppi item.");
        }

        List<EpubManifestItem> items = new(itemElements.Length);
        HashSet<string> ids = new(StringComparer.Ordinal);
        HashSet<string> resources = new(StringComparer.Ordinal);

        foreach (XElement element in itemElements)
        {
            string id = RequiredItemAttribute(element, "id");
            string href = RequiredItemAttribute(element, "href");
            string mediaType = RequiredItemAttribute(element, "media-type");
            if (!ids.Add(id))
            {
                throw Error(EpubPackageErrorCode.DuplicateManifestId, $"Manifest id duplicato: '{id}'.");
            }

            (OcfPath? localPath, Uri? remoteUri, string resourceKey) =
                ResolveManifestHref(packagePath, href);

            if (!resources.Add(resourceKey))
            {
                throw Error(
                    EpubPackageErrorCode.DuplicateManifestResource,
                    $"Più item del manifest risolvono alla stessa risorsa: '{href}'.");
            }

            if (localPath is not null)
            {
                if (localPath == packagePath)
                {
                    throw Error(
                        EpubPackageErrorCode.PackageDocumentSelfReference,
                        "Il Package Document non può essere incluso nel proprio manifest.");
                }

                if (requireLocalManifestResources && !container.Contains(localPath))
                {
                    throw Error(
                        EpubPackageErrorCode.ManifestResourceNotFound,
                        $"Risorsa locale del manifest non trovata: '{localPath.Value}'.");
                }
            }

            items.Add(new EpubManifestItem(
                id,
                href,
                mediaType,
                localPath,
                remoteUri,
                ParseProperties(OptionalAttribute(element, "properties")),
                OptionalAttribute(element, "fallback"),
                OptionalAttribute(element, "media-overlay")));
        }

        return items;
    }

    private static void ValidateManifestReferences(List<EpubManifestItem> manifest)
    {
        Dictionary<string, EpubManifestItem> byId = manifest.ToDictionary(item => item.Id, StringComparer.Ordinal);

        foreach (EpubManifestItem item in manifest)
        {
            if (item.FallbackId is not null && !byId.ContainsKey(item.FallbackId))
            {
                throw Error(
                    EpubPackageErrorCode.InvalidFallbackReference,
                    $"Fallback '{item.FallbackId}' di '{item.Id}' non esiste nel manifest.");
            }

            if (item.MediaOverlayId is not null && !byId.ContainsKey(item.MediaOverlayId))
            {
                throw Error(
                    EpubPackageErrorCode.InvalidMediaOverlayReference,
                    $"Media overlay '{item.MediaOverlayId}' di '{item.Id}' non esiste nel manifest.");
            }
        }

        foreach (EpubManifestItem item in manifest)
        {
            HashSet<string> visited = new(StringComparer.Ordinal) { item.Id };
            string? next = item.FallbackId;
            int depth = 0;
            while (next is not null)
            {
                depth++;
                if (depth > EpubPackageLimits.MaxFallbackDepth)
                {
                    throw Error(
                        EpubPackageErrorCode.FallbackDepthExceeded,
                        $"La catena fallback a partire da '{item.Id}' supera la profondità massima {EpubPackageLimits.MaxFallbackDepth}.");
                }

                if (!visited.Add(next))
                {
                    throw Error(
                        EpubPackageErrorCode.CircularFallback,
                        $"Catena fallback circolare rilevata a partire da '{item.Id}'.");
                }

                next = byId[next].FallbackId;
            }
        }
    }

    private static (List<EpubSpineItem> Spine, string? TocId, string? PageProgressionDirection) ReadSpine(
        XElement package,
        List<EpubManifestItem> manifest)
    {
        XElement spineElement = SingleChild(package, "spine", EpubPackageErrorCode.MissingSpine);
        XElement[] itemRefElements = spineElement.Elements(OpfNamespace + "itemref").ToArray();
        if (itemRefElements.Length == 0)
        {
            throw Error(EpubPackageErrorCode.EmptySpine, "Lo spine OPF non contiene itemref.");
        }

        if (itemRefElements.Length > EpubPackageLimits.MaxSpineItems)
        {
            throw Error(EpubPackageErrorCode.InvalidPackage, "Lo spine OPF contiene troppi itemref.");
        }

        HashSet<string> manifestIds = manifest.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        List<EpubSpineItem> spine = new(itemRefElements.Length);

        foreach (XElement element in itemRefElements)
        {
            string idRef = RequiredSpineAttribute(element, "idref");
            if (!manifestIds.Contains(idRef))
            {
                throw Error(
                    EpubPackageErrorCode.SpineManifestItemNotFound,
                    $"Lo spine fa riferimento all'id manifest inesistente '{idRef}'.");
            }

            string? linear = OptionalAttribute(element, "linear");
            if (linear is not null && linear is not ("yes" or "no"))
            {
                throw Error(
                    EpubPackageErrorCode.InvalidSpineItem,
                    $"Valore linear non valido per '{idRef}': '{linear}'.");
            }

            spine.Add(new EpubSpineItem(
                idRef,
                !string.Equals(linear, "no", StringComparison.Ordinal),
                ParseProperties(OptionalAttribute(element, "properties"))));
        }

        if (!spine.Exists(item => item.IsLinear))
        {
            throw Error(EpubPackageErrorCode.NoLinearSpineItem, "Lo spine deve contenere almeno un item lineare.");
        }

        string? pageProgressionDirection = OptionalAttribute(spineElement, "page-progression-direction");
        if (pageProgressionDirection is not null &&
            pageProgressionDirection is not ("default" or "ltr" or "rtl"))
        {
            throw Error(
                EpubPackageErrorCode.InvalidPageProgressionDirection,
                $"page-progression-direction non valido: '{pageProgressionDirection}'.");
        }

        return (spine, OptionalAttribute(spineElement, "toc"), pageProgressionDirection);
    }

    private static (OcfPath? LocalPath, Uri? RemoteUri, string ResourceKey) ResolveManifestHref(
        OcfPath packagePath,
        string href)
    {
        if (Uri.TryCreate(href, UriKind.Absolute, out Uri? absoluteUri))
        {
            if (absoluteUri.IsFile)
            {
                throw Error(EpubPackageErrorCode.InvalidManifestHref, "Gli URL file: non sono ammessi nel manifest EPUB.");
            }

            if (!string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw Error(
                    EpubPackageErrorCode.UnsupportedRemoteResourceScheme,
                    $"Lo schema URI '{absoluteUri.Scheme}' non è ammesso per una risorsa remota del manifest EPUB. Sono consentiti solo http e https.");
            }

            return (null, absoluteUri, $"remote:{absoluteUri.AbsoluteUri}");
        }

        if (href.Contains('#') || href.Contains('?'))
        {
            throw Error(
                EpubPackageErrorCode.InvalidManifestHref,
                $"L'href locale del manifest deve identificare una risorsa, senza query o fragment: '{href}'.");
        }

        int slash = packagePath.Value.LastIndexOf('/');
        string baseDirectory = slash < 0 ? string.Empty : packagePath.Value[..slash];
        string combined = baseDirectory.Length == 0 ? href : $"{baseDirectory}/{href}";

        try
        {
            OcfPath localPath = OcfPath.FromContainerReference(combined);
            return (localPath, null, $"local:{localPath.Value}");
        }
        catch (EpubContainerException exception)
        {
            throw new EpubPackageException(
                EpubPackageErrorCode.InvalidManifestHref,
                $"Href manifest locale non valido: '{href}'.",
                exception);
        }
    }

    private static XElement SingleChild(
        XElement package,
        string localName,
        EpubPackageErrorCode errorCode)
    {
        XElement[] elements = package.Elements(OpfNamespace + localName).ToArray();
        if (elements.Length != 1)
        {
            throw Error(errorCode, $"Il Package Document deve contenere esattamente un elemento '{localName}'.");
        }

        return elements[0];
    }

    private static void RequireDc(
        List<EpubDublinCoreMetadata> values,
        string name,
        EpubPackageErrorCode errorCode)
    {
        if (!values.Exists(value => string.Equals(value.Name, name, StringComparison.Ordinal)))
        {
            throw Error(errorCode, $"Metadata dc:{name} mancante.");
        }
    }

    private static string RequiredItemAttribute(XElement element, string name)
    {
        string? value = OptionalAttribute(element, name);
        if (value is null)
        {
            throw Error(EpubPackageErrorCode.InvalidManifestItem, $"Manifest item privo dell'attributo '{name}'.");
        }

        return value;
    }

    private static string RequiredSpineAttribute(XElement element, string name)
    {
        string? value = OptionalAttribute(element, name);
        if (value is null)
        {
            throw Error(EpubPackageErrorCode.InvalidSpineItem, $"Spine itemref privo dell'attributo '{name}'.");
        }

        return value;
    }

    private static string RequiredAttribute(XElement element, string name, EpubPackageErrorCode errorCode)
    {
        string? value = OptionalAttribute(element, name);
        if (value is null)
        {
            throw Error(errorCode, $"Attributo OPF obbligatorio '{name}' mancante.");
        }

        return value;
    }

    private static string? OptionalAttribute(XElement element, XName name)
    {
        string? value = (string?)element.Attribute(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static HashSet<string> ParseProperties(string? value) =>
        value is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet(StringComparer.Ordinal);


    private static bool IsValidModifiedTimestamp(string value)
    {
        if (value.Length != 20 || value[4] != '-' || value[7] != '-' || value[10] != 'T' ||
            value[13] != ':' || value[16] != ':' || value[19] != 'Z')
        {
            return false;
        }

        return DateTime.TryParseExact(
            value,
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out _);
    }

    private static string CollapseWhitespace(string value)
    {
        StringBuilder builder = new(value.Length);
        bool pendingSpace = false;

        foreach (char character in value.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static EpubPackageException Error(EpubPackageErrorCode errorCode, string message) =>
        new(errorCode, message);
}
