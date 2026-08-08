using System.Collections.ObjectModel;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Epub.Container;
using EbookReader.Epub.Content;
using EbookReader.Epub.Navigation;
using EbookReader.Epub.Package;

namespace EbookReader.Epub.Validation;

/// <summary>
/// Non-throwing facade for expected EPUB format/support failures.
/// Programmer errors and unexpected runtime failures are intentionally not swallowed.
/// </summary>
public static class EpubPublicationValidator
{
    public static EpubValidationResult Validate(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            using EpubContainer container = EpubContainer.Open(filePath);
            return Validate(container);
        }
        catch (EpubContainerException exception)
        {
            return Invalid(ContainerDiagnostic(exception));
        }
    }

    public static EpubValidationResult Validate(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using EpubContainer container = EpubContainer.Open(stream, leaveOpen);
            return Validate(container);
        }
        catch (EpubContainerException exception)
        {
            return Invalid(ContainerDiagnostic(exception));
        }
    }

    public static EpubValidationResult Validate(EpubContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        List<EpubDiagnostic> diagnostics = [];

        EpubProtectionReport protection;
        try
        {
            protection = EpubProtectionInspector.Inspect(container);
        }
        catch (EpubProtectionException exception)
        {
            diagnostics.Add(ProtectionDiagnostic(exception));
            return Invalid(diagnostics);
        }
        catch (EpubContainerException exception)
        {
            diagnostics.Add(ContainerDiagnostic(exception));
            return Invalid(diagnostics);
        }

        AddProtectionInformation(protection, diagnostics);

        if (protection.HasUnsupportedEncryption)
        {
            diagnostics.Add(new EpubDiagnostic(
                EpubDiagnosticCodes.UnsupportedEncryption,
                EpubDiagnosticSeverity.Error,
                EpubDiagnosticCategory.Protection,
                "L'EPUB contiene una o più risorse cifrate. EReader supporta solo EPUB senza DRM/cifratura; non viene tentata alcuna decrittazione."));
            return Unsupported(protection, diagnostics);
        }

        EpubPackageDocument package;
        try
        {
            package = EpubPackageReader.ReadForRecovery(container);
            EpubProtectionInspector.ValidateAgainstPackage(protection, package);
        }
        catch (EpubPackageException exception)
        {
            diagnostics.Add(PackageDiagnostic(exception));
            return Invalid(protection, diagnostics);
        }
        catch (EpubProtectionException exception)
        {
            diagnostics.Add(ProtectionDiagnostic(exception));
            return Invalid(protection, diagnostics);
        }
        catch (EpubContainerException exception)
        {
            diagnostics.Add(ContainerDiagnostic(exception));
            return Invalid(protection, diagnostics);
        }

        EpubNavigationDocument? navigation = TryReadNavigation(
            container,
            package,
            out EpubDiagnostic? navigationDiagnostic,
            out bool navigationFailureIsFatal);
        if (navigationFailureIsFatal)
        {
            diagnostics.Add(navigationDiagnostic
                ?? throw new InvalidOperationException("Una failure navigation fatal deve produrre una diagnostica."));
            return Invalid(protection, diagnostics);
        }

        EpubBookRecoveryResult recovery;
        try
        {
            recovery = EpubBookReader.ReadRecovering(container, package, navigation);
        }
        catch (EpubContentException exception)
        {
            diagnostics.Add(ContentDiagnostic(exception));
            return Invalid(protection, diagnostics);
        }
        catch (EpubContainerException exception)
        {
            diagnostics.Add(ContainerDiagnostic(exception));
            return Invalid(protection, diagnostics);
        }

        if (navigationDiagnostic is not null)
        {
            diagnostics.Add(navigationDiagnostic);
        }

        AddContentRecoveryDiagnostics(recovery.Issues, diagnostics);
        AddMissingResourceDiagnostics(container, package, recovery.Book, diagnostics);

        return new EpubValidationResult(
            EpubValidationStatus.Valid,
            recovery.Book,
            protection,
            diagnostics);
    }

    private static EpubNavigationDocument? TryReadNavigation(
        EpubContainer container,
        EpubPackageDocument package,
        out EpubDiagnostic? diagnostic,
        out bool failureIsFatal)
    {
        diagnostic = null;
        failureIsFatal = false;

        try
        {
            return EpubNavigationReader.Read(container, package);
        }
        catch (EpubNavigationException exception)
        {
            diagnostic = new EpubDiagnostic(
                EpubDiagnosticCodes.NavigationUnavailable,
                EpubDiagnosticSeverity.Error,
                EpubDiagnosticCategory.Navigation,
                $"Indice di navigazione non utilizzabile; la lettura continua senza TOC: {exception.Message}");
            return null;
        }
        catch (EpubContainerException exception) when (exception.ErrorCode == EpubContainerErrorCode.EntryNotFound)
        {
            diagnostic = new EpubDiagnostic(
                EpubDiagnosticCodes.NavigationUnavailable,
                EpubDiagnosticSeverity.Error,
                EpubDiagnosticCategory.Navigation,
                $"Indice di navigazione non disponibile nel contenitore; la lettura continua senza TOC: {exception.Message}");
            return null;
        }
        catch (EpubContainerException exception)
        {
            diagnostic = ContainerDiagnostic(exception);
            failureIsFatal = true;
            return null;
        }
    }

    private static void AddContentRecoveryDiagnostics(
        ReadOnlyCollection<EpubContentRecoveryIssue> issues,
        List<EpubDiagnostic> diagnostics)
    {
        foreach (EpubContentRecoveryIssue issue in issues)
        {
            diagnostics.Add(issue.Kind switch
            {
                EpubContentRecoveryKind.SupplementarySpineItemSkipped => new EpubDiagnostic(
                    EpubDiagnosticCodes.SupplementarySpineItemSkipped,
                    EpubDiagnosticSeverity.Error,
                    EpubDiagnosticCategory.Content,
                    issue.Message),
                EpubContentRecoveryKind.TableOfContentsDropped => new EpubDiagnostic(
                    EpubDiagnosticCodes.TableOfContentsDropped,
                    EpubDiagnosticSeverity.Error,
                    EpubDiagnosticCategory.Navigation,
                    issue.Message),
                _ => throw new ArgumentOutOfRangeException(nameof(issues), issue.Kind, "Recovery EPUB non supportata."),
            });
        }
    }

    private static void AddMissingResourceDiagnostics(
        EpubContainer container,
        EpubPackageDocument package,
        Book book,
        List<EpubDiagnostic> diagnostics)
    {
        HashSet<string> navigationResourceIds = GetNavigationResourceIds(package);
        HashSet<string> spineResourceIds = GetSpineResourceIds(package);
        HashSet<string> referencedImageIds = book.ReadingOrder
            .SelectMany(section => section.Blocks)
            .OfType<ImageBlock>()
            .Select(block => block.ResourceId.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (EpubManifestItem item in package.Manifest)
        {
            OcfPath? localPath = item.LocalPath;
            if (localPath is null || container.Contains(localPath))
            {
                continue;
            }

            if (navigationResourceIds.Contains(item.Id) || spineResourceIds.Contains(item.Id))
            {
                continue;
            }

            if (referencedImageIds.Contains(item.Id))
            {
                diagnostics.Add(new EpubDiagnostic(
                    EpubDiagnosticCodes.MissingReferencedImage,
                    EpubDiagnosticSeverity.Error,
                    EpubDiagnosticCategory.Content,
                    $"Immagine '{localPath.Value}' mancante: il testo resta leggibile e l'immagine rimane disponibile solo come placeholder/alt text."));
                continue;
            }

            diagnostics.Add(new EpubDiagnostic(
                EpubDiagnosticCodes.MissingOptionalResource,
                EpubDiagnosticSeverity.Warning,
                EpubDiagnosticCategory.Package,
                $"Risorsa locale opzionale '{localPath.Value}' dichiarata nel manifest ma assente; EReader continua senza cercare sostituti fuori dall'EPUB o su Internet."));
        }
    }

    private static HashSet<string> GetNavigationResourceIds(EpubPackageDocument package)
    {
        HashSet<string> result = package.Manifest
            .Where(item => item.HasProperty("nav"))
            .Select(item => item.Id)
            .ToHashSet(StringComparer.Ordinal);

        if (package.SpineTocId is not null)
        {
            result.Add(package.SpineTocId);
        }

        return result;
    }

    private static HashSet<string> GetSpineResourceIds(EpubPackageDocument package)
    {
        Dictionary<string, EpubManifestItem> byId = package.Manifest.ToDictionary(item => item.Id, StringComparer.Ordinal);
        HashSet<string> result = new(StringComparer.Ordinal);

        foreach (EpubSpineItem spineItem in package.Spine)
        {
            string? currentId = spineItem.IdRef;
            while (currentId is not null && result.Add(currentId))
            {
                currentId = byId[currentId].FallbackId;
            }
        }

        return result;
    }

    private static void AddProtectionInformation(
        EpubProtectionReport protection,
        List<EpubDiagnostic> diagnostics)
    {
        if (protection.HasRightsManagementDocument)
        {
            diagnostics.Add(new EpubDiagnostic(
                EpubDiagnosticCodes.RightsManagementMetadataPresent,
                EpubDiagnosticSeverity.Information,
                EpubDiagnosticCategory.Protection,
                "META-INF/rights.xml è presente. La sua presenza da sola non implica che il contenuto sia cifrato o DRM-protetto."));
        }

        int obfuscatedFonts = protection.Resources.Count(
            resource => resource.Kind == EpubProtectionKind.FontObfuscation);
        if (obfuscatedFonts > 0)
        {
            diagnostics.Add(new EpubDiagnostic(
                EpubDiagnosticCodes.FontObfuscationPresent,
                EpubDiagnosticSeverity.Information,
                EpubDiagnosticCategory.Protection,
                $"Sono dichiarati {obfuscatedFonts} font offuscati con l'algoritmo EPUB standard. L'offuscamento dei font non viene trattato come DRM e non è necessario per il rendering testuale CLI."));
        }
    }

    private static EpubDiagnostic ContainerDiagnostic(EpubContainerException exception) =>
        new(
            EnumCode("CONTAINER", (int)exception.ErrorCode),
            EpubDiagnosticSeverity.Error,
            EpubDiagnosticCategory.Container,
            exception.Message);

    private static EpubDiagnostic ProtectionDiagnostic(EpubProtectionException exception) =>
        new(
            EnumCode("PROTECTION", (int)exception.ErrorCode),
            EpubDiagnosticSeverity.Error,
            EpubDiagnosticCategory.Protection,
            exception.Message);

    private static EpubDiagnostic PackageDiagnostic(EpubPackageException exception) =>
        new(
            EnumCode("PACKAGE", (int)exception.ErrorCode),
            EpubDiagnosticSeverity.Error,
            EpubDiagnosticCategory.Package,
            exception.Message);

    private static EpubDiagnostic ContentDiagnostic(EpubContentException exception) =>
        new(
            EnumCode("CONTENT", (int)exception.ErrorCode),
            EpubDiagnosticSeverity.Error,
            EpubDiagnosticCategory.Content,
            exception.Message);

    private static string EnumCode(string area, int value) =>
        $"ER-EPUB-{area}-{value:D3}";

    private static EpubValidationResult Invalid(EpubDiagnostic diagnostic) =>
        Invalid([diagnostic]);

    private static EpubValidationResult Invalid(List<EpubDiagnostic> diagnostics) =>
        new(EpubValidationStatus.Invalid, null, null, diagnostics);

    private static EpubValidationResult Invalid(
        EpubProtectionReport protection,
        List<EpubDiagnostic> diagnostics) =>
        new(EpubValidationStatus.Invalid, null, protection, diagnostics);

    private static EpubValidationResult Unsupported(
        EpubProtectionReport protection,
        List<EpubDiagnostic> diagnostics) =>
        new(EpubValidationStatus.Unsupported, null, protection, diagnostics);
}
