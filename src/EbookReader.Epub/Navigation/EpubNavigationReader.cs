using System.Text;
using System.Xml;
using System.Xml.Linq;
using EbookReader.Epub.Container;
using EbookReader.Epub.Package;

namespace EbookReader.Epub.Navigation;

/// <summary>
/// Reads EPUB 3 Navigation Documents and EPUB 2 NCX files into one EPUB-specific,
/// source-normalized navigation model.
/// </summary>
public static class EpubNavigationReader
{
    private const string XhtmlNamespaceValue = "http://www.w3.org/1999/xhtml";
    private const string EpubNamespaceValue = "http://www.idpf.org/2007/ops";
    private const string NcxNamespaceValue = "http://www.daisy.org/z3986/2005/ncx/";
    private const string XhtmlMediaType = "application/xhtml+xml";
    private const string NcxMediaType = "application/x-dtbncx+xml";
    private const string CanonicalNcxPublicId = "-//NISO//DTD ncx 2005-1//EN";
    private const string CanonicalNcxSystemId = "http://www.daisy.org/z3986/2005/ncx-2005-1.dtd";

    private static readonly XNamespace XhtmlNamespace = XhtmlNamespaceValue;
    private static readonly XNamespace EpubNamespace = EpubNamespaceValue;
    private static readonly XNamespace NcxNamespace = NcxNamespaceValue;

    public static EpubNavigationDocument Read(EpubContainer container, EpubPackageDocument package)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(package);

        HashSet<string> topLevelContentPaths = BuildTopLevelContentPaths(package);
        return package.IsEpub3
            ? ReadEpub3(container, package, topLevelContentPaths)
            : ReadEpub2(container, package, topLevelContentPaths);
    }

    private static HashSet<string> BuildTopLevelContentPaths(EpubPackageDocument package)
    {
        Dictionary<string, EpubManifestItem> byId = package.Manifest.ToDictionary(item => item.Id, StringComparer.Ordinal);
        HashSet<string> paths = new(StringComparer.Ordinal);

        foreach (EpubSpineItem spineItem in package.Spine)
        {
            EpubManifestItem current = byId[spineItem.IdRef];
            HashSet<string> visited = new(StringComparer.Ordinal);
            while (visited.Add(current.Id))
            {
                if (current.LocalPath is not null)
                {
                    paths.Add(current.LocalPath.Value);
                }

                if (current.FallbackId is null)
                {
                    break;
                }

                current = byId[current.FallbackId];
            }
        }

        return paths;
    }

    private static EpubNavigationDocument ReadEpub3(
        EpubContainer container,
        EpubPackageDocument package,
        HashSet<string> allowedTargetPaths)
    {
        EpubManifestItem[] navigationItems = package.Manifest
            .Where(item => item.HasProperty("nav"))
            .ToArray();

        if (navigationItems.Length == 0)
        {
            throw Error(
                EpubNavigationErrorCode.NavigationDocumentNotFound,
                "Un EPUB 3 deve dichiarare esattamente un Navigation Document con properties='nav'.");
        }

        if (navigationItems.Length != 1)
        {
            throw Error(
                EpubNavigationErrorCode.MultipleNavigationDocuments,
                "Un EPUB 3 non può dichiarare più di un Navigation Document con properties='nav'.");
        }

        EpubManifestItem item = navigationItems[0];
        if (!string.Equals(item.MediaType, XhtmlMediaType, StringComparison.OrdinalIgnoreCase))
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNavigationMediaType,
                $"Il Navigation Document deve avere media-type '{XhtmlMediaType}'.");
        }

        OcfPath sourcePath = RequireLocalSource(item, EpubNavigationErrorCode.NavigationSourceMustBeLocal);
        using Stream stream = container.OpenEntry(sourcePath);
        XDocument document = LoadXml(
            stream,
            EpubNavigationErrorCode.NavigationDocumentTooLarge,
            EpubNavigationErrorCode.InvalidNavigationXhtml,
            "Navigation Document XHTML",
            allowDoctype: false);

        XElement? root = document.Root;
        if (root is null ||
            !string.Equals(root.Name.LocalName, "html", StringComparison.Ordinal) ||
            root.Name.Namespace != XhtmlNamespace)
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNavigationXhtmlNamespace,
                $"Il Navigation Document deve avere root XHTML html nel namespace '{XhtmlNamespaceValue}'.");
        }

        List<EpubNavigationList> lists = [];
        NavigationParseState state = new();
        XElement toc = RequireSingleTypedNav(root, "toc", required: true)!;
        lists.Add(ParseXhtmlNavigationList(
            toc,
            EpubNavigationListKind.TableOfContents,
            sourcePath,
            container,
            allowedTargetPaths,
            state));

        XElement? pageList = RequireSingleTypedNav(root, "page-list", required: false);
        if (pageList is not null)
        {
            lists.Add(ParseXhtmlNavigationList(
                pageList,
                EpubNavigationListKind.PageList,
                sourcePath,
                container,
                allowedTargetPaths,
                state));
        }

        XElement? landmarks = RequireSingleTypedNav(root, "landmarks", required: false);
        if (landmarks is not null)
        {
            lists.Add(ParseXhtmlNavigationList(
                landmarks,
                EpubNavigationListKind.Landmarks,
                sourcePath,
                container,
                allowedTargetPaths,
                state));
        }

        return new EpubNavigationDocument(EpubNavigationSourceKind.Epub3NavigationDocument, sourcePath, lists);
    }

    private static EpubNavigationDocument ReadEpub2(
        EpubContainer container,
        EpubPackageDocument package,
        HashSet<string> allowedTargetPaths)
    {
        if (package.SpineTocId is null)
        {
            throw Error(
                EpubNavigationErrorCode.MissingNcxReference,
                "Lo spine di un EPUB 2 deve identificare l'NCX tramite l'attributo toc.");
        }

        EpubManifestItem? item = package.Manifest.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, package.SpineTocId, StringComparison.Ordinal));
        if (item is null)
        {
            throw Error(
                EpubNavigationErrorCode.NcxManifestItemNotFound,
                $"Lo spine EPUB 2 fa riferimento all'NCX inesistente '{package.SpineTocId}'.");
        }

        if (!string.Equals(item.MediaType, NcxMediaType, StringComparison.OrdinalIgnoreCase))
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNcxMediaType,
                $"La risorsa NCX deve avere media-type '{NcxMediaType}'.");
        }

        OcfPath sourcePath = RequireLocalSource(item, EpubNavigationErrorCode.NavigationSourceMustBeLocal);
        using Stream stream = container.OpenEntry(sourcePath);
        XDocument document = LoadXml(
            stream,
            EpubNavigationErrorCode.NcxDocumentTooLarge,
            EpubNavigationErrorCode.InvalidNcxXml,
            "NCX",
            allowDoctype: true);

        XElement? root = document.Root;
        if (root is null ||
            !string.Equals(root.Name.LocalName, "ncx", StringComparison.Ordinal) ||
            root.Name.Namespace != NcxNamespace)
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNcxNamespace,
                $"L'NCX deve avere root ncx nel namespace '{NcxNamespaceValue}'.");
        }

        string? ncxVersion = OptionalAttribute(root, "version");
        if (!string.Equals(ncxVersion, "2005-1", StringComparison.Ordinal))
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNcxXml,
                $"Versione NCX non supportata: '{ncxVersion ?? "<mancante>"}'. Atteso '2005-1'.");
        }

        bool requiresPlayOrder = ValidateNcxDoctype(document);

        XElement[] navMaps = root.Elements(NcxNamespace + "navMap").ToArray();
        if (navMaps.Length != 1)
        {
            throw Error(
                EpubNavigationErrorCode.MissingNcxNavMap,
                "L'NCX deve contenere esattamente un navMap.");
        }

        XElement[] navPoints = navMaps[0].Elements(NcxNamespace + "navPoint").ToArray();
        if (navPoints.Length == 0)
        {
            throw Error(EpubNavigationErrorCode.MissingNcxNavMap, "Il navMap NCX non può essere vuoto.");
        }

        NavigationParseState state = new();
        HashSet<string> ncxIds = new(StringComparer.Ordinal);
        List<EpubNavigationNode> items = new(navPoints.Length);
        foreach (XElement navPoint in navPoints)
        {
            items.Add(ParseNcxNavPoint(
                navPoint,
                depth: 1,
                sourcePath,
                container,
                allowedTargetPaths,
                state,
                ncxIds,
                requiresPlayOrder));
        }

        List<EpubNavigationList> lists =
        [
            new EpubNavigationList(EpubNavigationListKind.TableOfContents, null, items),
        ];

        return new EpubNavigationDocument(EpubNavigationSourceKind.Epub2Ncx, sourcePath, lists);
    }

    private static XElement? RequireSingleTypedNav(XElement root, string type, bool required)
    {
        XElement[] matches = root
            .Descendants(XhtmlNamespace + "nav")
            .Where(element => HasToken((string?)element.Attribute(EpubNamespace + "type"), type))
            .ToArray();

        if (matches.Length > 1)
        {
            throw Error(
                EpubNavigationErrorCode.DuplicateNavigationAid,
                $"Il Navigation Document contiene più nav epub:type='{type}'.");
        }

        if (matches.Length == 0 && required)
        {
            throw Error(
                EpubNavigationErrorCode.MissingTableOfContents,
                "Il Navigation Document EPUB 3 deve contenere esattamente un nav epub:type='toc'.");
        }

        return matches.Length == 0 ? null : matches[0];
    }

    private static EpubNavigationList ParseXhtmlNavigationList(
        XElement nav,
        EpubNavigationListKind kind,
        OcfPath sourcePath,
        EpubContainer container,
        HashSet<string> allowedTargetPaths,
        NavigationParseState state)
    {
        XElement[] directElements = nav.Elements().ToArray();
        if (directElements.Any(element => element.Name.Namespace != XhtmlNamespace))
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNavigationStructure,
                "Un nav specializzato contiene elementi diretti fuori dal namespace XHTML.");
        }

        XElement? heading = null;
        XElement orderedList;
        if (directElements.Length == 1 && directElements[0].Name == XhtmlNamespace + "ol")
        {
            orderedList = directElements[0];
        }
        else if (directElements.Length == 2 &&
                 IsHeadingName(directElements[0].Name.LocalName) &&
                 directElements[1].Name == XhtmlNamespace + "ol")
        {
            heading = directElements[0];
            orderedList = directElements[1];
        }
        else
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNavigationStructure,
                "Un nav specializzato deve contenere, in ordine, un heading XHTML opzionale e un solo ol.");
        }

        string? listLabel = heading is null ? null : ReadLabel(heading);
        List<EpubNavigationNode> items = ParseXhtmlOrderedList(
            orderedList,
            depth: 1,
            sourcePath,
            container,
            allowedTargetPaths,
            state);

        return new EpubNavigationList(kind, listLabel, items);
    }

    private static List<EpubNavigationNode> ParseXhtmlOrderedList(
        XElement orderedList,
        int depth,
        OcfPath sourcePath,
        EpubContainer container,
        HashSet<string> allowedTargetPaths,
        NavigationParseState state)
    {
        CheckDepth(depth);
        XElement[] listItems = orderedList.Elements().ToArray();
        if (listItems.Length == 0 || listItems.Any(element => element.Name != XhtmlNamespace + "li"))
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNavigationStructure,
                "Un ol di navigazione deve contenere uno o più li XHTML e nessun altro elemento diretto.");
        }

        List<EpubNavigationNode> result = new(listItems.Length);
        foreach (XElement item in listItems)
        {
            IncrementNodeCount(state);
            XElement[] directElements = item.Elements().ToArray();
            if (directElements.Length is < 1 or > 2 ||
                (directElements[0].Name != XhtmlNamespace + "a" &&
                 directElements[0].Name != XhtmlNamespace + "span") ||
                (directElements.Length == 2 && directElements[1].Name != XhtmlNamespace + "ol"))
            {
                throw Error(
                    EpubNavigationErrorCode.InvalidNavigationStructure,
                    "Ogni li deve contenere, in ordine, un a oppure span e al massimo un ol annidato.");
            }

            XElement labelElement = directElements[0];
            string label = ReadLabel(labelElement);
            EpubNavigationTarget? target = null;
            if (labelElement.Name == XhtmlNamespace + "a")
            {
                string? href = OptionalAttribute(labelElement, "href");
                if (href is null)
                {
                    throw Error(
                        EpubNavigationErrorCode.InvalidNavigationHref,
                        "Un link di navigazione deve dichiarare href.");
                }

                target = ResolveTarget(sourcePath, href, container, allowedTargetPaths);
            }

            List<EpubNavigationNode> children = directElements.Length == 1
                ? []
                : ParseXhtmlOrderedList(
                    directElements[1],
                    depth + 1,
                    sourcePath,
                    container,
                    allowedTargetPaths,
                    state);

            if (target is null && children.Count == 0)
            {
                throw Error(
                    EpubNavigationErrorCode.InvalidNavigationStructure,
                    "Un nodo span senza target deve raggruppare almeno un nodo figlio.");
            }

            HashSet<string> types = ParseTokens((string?)labelElement.Attribute(EpubNamespace + "type"));
            result.Add(new EpubNavigationNode(label, target, types, children));
        }

        return result;
    }

    private static EpubNavigationNode ParseNcxNavPoint(
        XElement navPoint,
        int depth,
        OcfPath sourcePath,
        EpubContainer container,
        HashSet<string> allowedTargetPaths,
        NavigationParseState state,
        HashSet<string> ncxIds,
        bool requiresPlayOrder)
    {
        CheckDepth(depth);
        IncrementNodeCount(state);

        string? id = OptionalAttribute(navPoint, "id");
        if (id is null)
        {
            throw Error(EpubNavigationErrorCode.InvalidNcxNavPoint, "Un navPoint NCX deve avere un id.");
        }

        if (!ncxIds.Add(id))
        {
            throw Error(EpubNavigationErrorCode.DuplicateNcxId, $"navPoint NCX id duplicato: '{id}'.");
        }

        if (requiresPlayOrder && OptionalAttribute(navPoint, "playOrder") is null)
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNcxNavPoint,
                $"Il navPoint '{id}' deve dichiarare playOrder quando l'NCX usa il DOCTYPE canonico.");
        }

        XElement[] labels = navPoint.Elements(NcxNamespace + "navLabel").ToArray();
        if (labels.Length == 0)
        {
            throw Error(EpubNavigationErrorCode.InvalidNcxNavPoint, $"navPoint '{id}' privo di navLabel.");
        }

        XElement[] texts = labels[0].Elements(NcxNamespace + "text").ToArray();
        if (texts.Length != 1)
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNcxNavPoint,
                $"Il primo navLabel del navPoint '{id}' deve contenere esattamente un text.");
        }

        string label = NormalizeLabel(texts[0].Value);
        XElement[] contents = navPoint.Elements(NcxNamespace + "content").ToArray();
        if (contents.Length != 1)
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNcxNavPoint,
                $"navPoint '{id}' deve contenere esattamente un content.");
        }

        string? src = OptionalAttribute(contents[0], "src");
        if (src is null)
        {
            throw Error(EpubNavigationErrorCode.InvalidNcxNavPoint, $"content del navPoint '{id}' privo di src.");
        }

        EpubNavigationTarget target = ResolveTarget(sourcePath, src, container, allowedTargetPaths);
        XElement[] childPoints = navPoint.Elements(NcxNamespace + "navPoint").ToArray();
        List<EpubNavigationNode> children = new(childPoints.Length);
        foreach (XElement child in childPoints)
        {
            children.Add(ParseNcxNavPoint(
                child,
                depth + 1,
                sourcePath,
                container,
                allowedTargetPaths,
                state,
                ncxIds,
                requiresPlayOrder));
        }

        return new EpubNavigationNode(label, target, new HashSet<string>(StringComparer.Ordinal), children);
    }

    private static EpubNavigationTarget ResolveTarget(
        OcfPath sourcePath,
        string href,
        EpubContainer container,
        HashSet<string> allowedTargetPaths)
    {
        if (Uri.TryCreate(href, UriKind.Absolute, out _))
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNavigationHref,
                "I TOC, page-list, landmarks e navMap NCX devono puntare a contenuto interno alla pubblicazione.");
        }

        int hashIndex = href.IndexOf('#');
        string pathPart = hashIndex < 0 ? href : href[..hashIndex];
        string? fragment = hashIndex < 0 ? null : DecodeFragment(href[(hashIndex + 1)..]);
        if (pathPart.Contains('?'))
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNavigationHref,
                $"I target locali con query non sono supportati: '{href}'.");
        }

        OcfPath localPath;
        if (pathPart.Length == 0)
        {
            localPath = sourcePath;
        }
        else
        {
            int slash = sourcePath.Value.LastIndexOf('/');
            string baseDirectory = slash < 0 ? string.Empty : sourcePath.Value[..slash];
            string combined = baseDirectory.Length == 0 ? pathPart : $"{baseDirectory}/{pathPart}";
            try
            {
                localPath = OcfPath.FromContainerReference(combined);
            }
            catch (EpubContainerException exception)
            {
                throw new EpubNavigationException(
                    EpubNavigationErrorCode.InvalidNavigationHref,
                    $"Target di navigazione locale non valido: '{href}'.",
                    exception);
            }
        }

        if (!container.Contains(localPath) || !allowedTargetPaths.Contains(localPath.Value))
        {
            throw Error(
                EpubNavigationErrorCode.NavigationTargetNotFound,
                $"Il target di navigazione locale non è una risorsa dichiarata esistente: '{localPath.Value}'.");
        }

        return new EpubNavigationTarget(href, localPath, fragment);
    }

    private static OcfPath RequireLocalSource(EpubManifestItem item, EpubNavigationErrorCode errorCode)
    {
        if (item.LocalPath is null)
        {
            throw Error(errorCode, $"La risorsa di navigazione '{item.Id}' deve essere locale al contenitore EPUB.");
        }

        return item.LocalPath;
    }

    private static bool ValidateNcxDoctype(XDocument document)
    {
        XDocumentType? documentType = document.DocumentType;
        if (documentType is null)
        {
            return false;
        }

        if (!string.Equals(documentType.Name, "ncx", StringComparison.Ordinal) ||
            !string.Equals(documentType.PublicId, CanonicalNcxPublicId, StringComparison.Ordinal) ||
            !string.Equals(documentType.SystemId, CanonicalNcxSystemId, StringComparison.Ordinal) ||
            !string.IsNullOrWhiteSpace(documentType.InternalSubset))
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNcxXml,
                "L'NCX può dichiarare soltanto il DOCTYPE canonico NISO/DAISY NCX 2005-1.");
        }

        return true;
    }

    private static XDocument LoadXml(
        Stream stream,
        EpubNavigationErrorCode tooLargeCode,
        EpubNavigationErrorCode invalidXmlCode,
        string description,
        bool allowDoctype)
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
            if (total > EpubNavigationLimits.MaxDocumentBytes)
            {
                throw Error(
                    tooLargeCode,
                    $"{description} supera il limite di {EpubNavigationLimits.MaxDocumentBytes} byte.");
            }

            bounded.Write(buffer.AsSpan(0, read));
        }

        bounded.Position = 0;
        XmlReaderSettings settings = new()
        {
            DtdProcessing = allowDoctype ? DtdProcessing.Parse : DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = EpubNavigationLimits.MaxDocumentBytes,
            MaxCharactersFromEntities = 4_096,
        };

        try
        {
            using XmlReader reader = XmlReader.Create(bounded, settings);
            return XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            throw new EpubNavigationException(
                invalidXmlCode,
                $"{description} non è XML valido o contiene costrutti XML non consentiti.",
                exception);
        }
    }

    private static string ReadLabel(XElement element)
    {
        StringBuilder builder = new();
        AppendLabelContent(element, builder);
        return NormalizeLabel(builder.ToString());
    }

    private static void AppendLabelContent(XElement element, StringBuilder builder)
    {
        foreach (XNode node in element.Nodes())
        {
            if (node is XText text)
            {
                builder.Append(text.Value);
                continue;
            }

            if (node is not XElement child)
            {
                continue;
            }

            if (child.Name.Namespace == XhtmlNamespace &&
                string.Equals(child.Name.LocalName, "img", StringComparison.Ordinal))
            {
                string? alternative = OptionalAttribute(child, "alt") ?? OptionalAttribute(child, "title");
                if (alternative is not null)
                {
                    builder.Append(' ');
                    builder.Append(alternative);
                    builder.Append(' ');
                }

                continue;
            }

            AppendLabelContent(child, builder);
        }
    }

    private static string NormalizeLabel(string value)
    {
        string normalized = CollapseWhitespace(value);
        if (normalized.Length == 0)
        {
            throw Error(EpubNavigationErrorCode.EmptyNavigationLabel, "Un'etichetta di navigazione non può essere vuota.");
        }

        if (normalized.Length > EpubNavigationLimits.MaxLabelLength)
        {
            throw Error(
                EpubNavigationErrorCode.InvalidNavigationStructure,
                $"Un'etichetta di navigazione supera {EpubNavigationLimits.MaxLabelLength} caratteri.");
        }

        return normalized;
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

    private static string? DecodeFragment(string rawFragment)
    {
        string value = rawFragment.StartsWith('#') ? rawFragment[1..] : rawFragment;
        if (value.Length == 0)
        {
            return null;
        }

        ValidatePercentEscapes(value);
        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(value);
        }
        catch (UriFormatException exception)
        {
            throw new EpubNavigationException(
                EpubNavigationErrorCode.InvalidNavigationHref,
                $"Fragment non valido: '{rawFragment}'.",
                exception);
        }

        if (decoded.Any(char.IsControl))
        {
            throw Error(EpubNavigationErrorCode.InvalidNavigationHref, "Il fragment contiene caratteri di controllo.");
        }

        return decoded;
    }

    private static void ValidatePercentEscapes(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] != '%')
            {
                continue;
            }

            if (index + 2 >= value.Length ||
                !IsHexDigit(value[index + 1]) ||
                !IsHexDigit(value[index + 2]))
            {
                throw Error(EpubNavigationErrorCode.InvalidNavigationHref, $"Escape percentuale non valido: '{value}'.");
            }

            index += 2;
        }
    }

    private static HashSet<string> ParseTokens(string? value) =>
        value is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet(StringComparer.Ordinal);

    private static bool HasToken(string? value, string token) =>
        value is not null &&
        value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Contains(token, StringComparer.Ordinal);

    private static bool IsHeadingName(string localName) =>
        localName is "h1" or "h2" or "h3" or "h4" or "h5" or "h6";

    private static string? OptionalAttribute(XElement element, XName name)
    {
        string? value = (string?)element.Attribute(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void CheckDepth(int depth)
    {
        if (depth > EpubNavigationLimits.MaxDepth)
        {
            throw Error(
                EpubNavigationErrorCode.NavigationDepthExceeded,
                $"La gerarchia di navigazione supera la profondità massima {EpubNavigationLimits.MaxDepth}.");
        }
    }

    private static void IncrementNodeCount(NavigationParseState state)
    {
        state.NodeCount++;
        if (state.NodeCount > EpubNavigationLimits.MaxNodes)
        {
            throw Error(
                EpubNavigationErrorCode.TooManyNavigationNodes,
                $"La navigazione supera il limite di {EpubNavigationLimits.MaxNodes} nodi.");
        }
    }

    private static bool IsHexDigit(char value) =>
        value is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';

    private static EpubNavigationException Error(EpubNavigationErrorCode errorCode, string message) =>
        new(errorCode, message);

    private sealed class NavigationParseState
    {
        public int NodeCount { get; set; }
    }
}
