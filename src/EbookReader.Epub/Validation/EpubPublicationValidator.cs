using EbookReader.Domain.Books;
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
            package = EpubPackageReader.Read(container);
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

        EpubNavigationDocument navigation;
        try
        {
            navigation = EpubNavigationReader.Read(container, package);
        }
        catch (EpubNavigationException exception)
        {
            diagnostics.Add(NavigationDiagnostic(exception));
            return Invalid(protection, diagnostics);
        }

        Book book;
        try
        {
            book = EpubBookReader.Read(container, package, navigation);
        }
        catch (EpubContentException exception)
        {
            diagnostics.Add(ContentDiagnostic(exception));
            return Invalid(protection, diagnostics);
        }

        return new EpubValidationResult(
            EpubValidationStatus.Valid,
            book,
            protection,
            diagnostics);
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

    private static EpubDiagnostic NavigationDiagnostic(EpubNavigationException exception) =>
        new(
            EnumCode("NAVIGATION", (int)exception.ErrorCode),
            EpubDiagnosticSeverity.Error,
            EpubDiagnosticCategory.Navigation,
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
