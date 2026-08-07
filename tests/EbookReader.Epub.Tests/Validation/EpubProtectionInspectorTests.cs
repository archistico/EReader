using EbookReader.Epub.Container;
using EbookReader.Epub.Package;
using EbookReader.Epub.Validation;

namespace EbookReader.Epub.Tests.Validation;

public sealed class EpubProtectionInspectorTests
{
    [Fact]
    public void MissingEncryptionDocumentProducesEmptyReport()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create(includeRights: true);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionReport report = EpubProtectionInspector.Inspect(container);

        Assert.False(report.HasEncryptionDocument);
        Assert.True(report.HasRightsManagementDocument);
        Assert.Empty(report.Resources);
    }

    [Fact]
    public void StandardFontObfuscationIsRecognizedWithoutDecrypting()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml(ValidationFixtureFactory.FontPath);
        using MemoryStream stream = ValidationFixtureFactory.Create(
            encryptionXml: encryption,
            includeFontFile: true,
            declareFont: true);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionReport report = EpubProtectionInspector.Inspect(container);
        EpubPackageDocument package = EpubPackageReader.Read(container);
        EpubProtectionInspector.ValidateAgainstPackage(report, package);

        EpubProtectedResource resource = Assert.Single(report.Resources);
        Assert.Equal(EpubProtectionKind.FontObfuscation, resource.Kind);
        Assert.Equal(ValidationFixtureFactory.FontPath, resource.Path.Value);
        Assert.True(report.HasFontObfuscation);
        Assert.False(report.HasUnsupportedEncryption);
    }

    [Fact]
    public void LegacyAdobeFontObfuscationIsRecognizedForCompatibility()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml(
            ValidationFixtureFactory.FontPath,
            EpubProtectionInspector.AdobeFontObfuscationAlgorithm);
        using MemoryStream stream = ValidationFixtureFactory.Create(
            encryptionXml: encryption,
            includeFontFile: true,
            declareFont: true);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionReport report = EpubProtectionInspector.Inspect(container);
        EpubPackageDocument package = EpubPackageReader.Read(container);
        EpubProtectionInspector.ValidateAgainstPackage(report, package);

        EpubProtectedResource resource = Assert.Single(report.Resources);
        Assert.Equal(EpubProtectionKind.FontObfuscation, resource.Kind);
        Assert.Equal(EpubProtectionInspector.AdobeFontObfuscationAlgorithm, resource.Algorithm);
    }

    [Fact]
    public void UnknownEncryptionAlgorithmIsClassifiedAsUnsupported()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml(
            ValidationFixtureFactory.ChapterPath,
            "http://www.w3.org/2001/04/xmlenc#aes256-cbc");
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionReport report = EpubProtectionInspector.Inspect(container);

        EpubProtectedResource resource = Assert.Single(report.Resources);
        Assert.Equal(EpubProtectionKind.UnsupportedEncryption, resource.Kind);
        Assert.True(report.HasUnsupportedEncryption);
    }

    [Fact]
    public void MissingEncryptionMethodIsConservativelyUnsupported()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml(
            ValidationFixtureFactory.ChapterPath,
            algorithm: null);
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionReport report = EpubProtectionInspector.Inspect(container);

        Assert.Equal(EpubProtectionKind.UnsupportedEncryption, Assert.Single(report.Resources).Kind);
    }

    [Fact]
    public void MalformedEncryptionXmlIsRejected()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: "<encryption>");
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.Inspect(container));

        Assert.Equal(EpubProtectionErrorCode.InvalidProtectionXml, exception.ErrorCode);
    }

    [Fact]
    public void EncryptionDoctypeIsRejected()
    {
        const string encryption = """
        <!DOCTYPE encryption [<!ENTITY x "boom">]>
        <encryption xmlns="urn:oasis:names:tc:opendocument:xmlns:container"
                    xmlns:enc="http://www.w3.org/2001/04/xmlenc#">
          <enc:EncryptedData><enc:CipherData><enc:CipherReference URI="EPUB/Text/ch1.xhtml" /></enc:CipherData></enc:EncryptedData>
        </encryption>
        """;
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.Inspect(container));

        Assert.Equal(EpubProtectionErrorCode.InvalidProtectionXml, exception.ErrorCode);
    }

    [Fact]
    public void WrongEncryptionNamespaceIsRejected()
    {
        const string encryption = """
        <encryption xmlns="urn:wrong"
                    xmlns:enc="http://www.w3.org/2001/04/xmlenc#">
          <enc:EncryptedData><enc:CipherData><enc:CipherReference URI="EPUB/Text/ch1.xhtml" /></enc:CipherData></enc:EncryptedData>
        </encryption>
        """;
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.Inspect(container));

        Assert.Equal(EpubProtectionErrorCode.InvalidProtectionNamespace, exception.ErrorCode);
    }

    [Fact]
    public void EncryptionDocumentWithoutEncryptedDataIsRejected()
    {
        const string encryption = """
        <encryption xmlns="urn:oasis:names:tc:opendocument:xmlns:container" />
        """;
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.Inspect(container));

        Assert.Equal(EpubProtectionErrorCode.MissingEncryptedData, exception.ErrorCode);
    }

    [Fact]
    public void MissingCipherReferenceIsRejected()
    {
        const string encryption = """
        <encryption xmlns="urn:oasis:names:tc:opendocument:xmlns:container"
                    xmlns:enc="http://www.w3.org/2001/04/xmlenc#">
          <enc:EncryptedData><enc:CipherData /></enc:EncryptedData>
        </encryption>
        """;
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.Inspect(container));

        Assert.Equal(EpubProtectionErrorCode.InvalidCipherReference, exception.ErrorCode);
    }

    [Fact]
    public void CipherReferenceResolvesAgainstContainerRoot()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml("EPUB/fonts/test%2Ettf");
        using MemoryStream stream = ValidationFixtureFactory.Create(
            encryptionXml: encryption,
            includeFontFile: true,
            declareFont: true);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionReport report = EpubProtectionInspector.Inspect(container);

        Assert.Equal(ValidationFixtureFactory.FontPath, Assert.Single(report.Resources).Path.Value);
    }

    [Fact]
    public void CipherReferenceTraversalBeyondContainerRootIsRejected()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml("../EPUB/Text/ch1.xhtml");
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.Inspect(container));

        Assert.Equal(EpubProtectionErrorCode.InvalidCipherReference, exception.ErrorCode);
    }

    [Fact]
    public void MissingProtectedResourceIsRejected()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml("EPUB/missing.bin");
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.Inspect(container));

        Assert.Equal(EpubProtectionErrorCode.ProtectedResourceNotFound, exception.ErrorCode);
    }

    [Fact]
    public void PackageDocumentCannotBeDeclaredEncrypted()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml("EPUB/package.opf");
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.Inspect(container));

        Assert.Equal(EpubProtectionErrorCode.ForbiddenProtectedResource, exception.ErrorCode);
    }

    [Fact]
    public void DuplicateProtectedResourceIsRejected()
    {
        string one = ValidationFixtureFactory.EncryptionXml(ValidationFixtureFactory.ChapterPath);
        string encryptedData = """
        <enc:EncryptedData xmlns:enc="http://www.w3.org/2001/04/xmlenc#">
          <enc:CipherData><enc:CipherReference URI="EPUB/Text/ch1.xhtml" /></enc:CipherData>
        </enc:EncryptedData>
        """;
        string encryption = one.Replace("</encryption>", encryptedData + "</encryption>", StringComparison.Ordinal);
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.Inspect(container));

        Assert.Equal(EpubProtectionErrorCode.DuplicateProtectedResource, exception.ErrorCode);
    }

    [Fact]
    public void ObfuscatedFontMustBeDeclaredInManifest()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml(ValidationFixtureFactory.FontPath);
        using MemoryStream stream = ValidationFixtureFactory.Create(
            encryptionXml: encryption,
            includeFontFile: true,
            declareFont: false);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);
        EpubProtectionReport report = EpubProtectionInspector.Inspect(container);
        EpubPackageDocument package = EpubPackageReader.Read(container);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.ValidateAgainstPackage(report, package));

        Assert.Equal(EpubProtectionErrorCode.FontObfuscationResourceNotInManifest, exception.ErrorCode);
    }

    [Fact]
    public void FontObfuscationCannotTargetNonFontManifestResource()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml(ValidationFixtureFactory.FontPath);
        using MemoryStream stream = ValidationFixtureFactory.Create(
            encryptionXml: encryption,
            includeFontFile: true,
            declareFont: true,
            fontMediaType: "image/jpeg");
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);
        EpubProtectionReport report = EpubProtectionInspector.Inspect(container);
        EpubPackageDocument package = EpubPackageReader.Read(container);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.ValidateAgainstPackage(report, package));

        Assert.Equal(EpubProtectionErrorCode.FontObfuscationTargetNotFont, exception.ErrorCode);
    }
    [Fact]
    public void EncodedPathSeparatorInCipherReferenceIsRejected()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml("EPUB%2FText/ch1.xhtml");
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.Inspect(container));

        Assert.Equal(EpubProtectionErrorCode.InvalidCipherReference, exception.ErrorCode);
    }

    [Fact]
    public void OversizedEncryptionDocumentIsRejected()
    {
        string encryption = "<encryption>" + new string('x', 1024 * 1024) + "</encryption>";
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: encryption);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubProtectionException exception = Assert.Throws<EpubProtectionException>(
            () => EpubProtectionInspector.Inspect(container));

        Assert.Equal(EpubProtectionErrorCode.ProtectionDocumentTooLarge, exception.ErrorCode);
    }

}
