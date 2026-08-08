using System.Buffers.Binary;
using System.Text;
using EbookReader.Epub.Validation;

namespace EbookReader.Epub.Tests.Validation;

public sealed class EpubPublicationValidatorTests
{
    [Fact]
    public void ValidPublicationProducesReadableDomainBook()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create();

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.True(result.CanRead);
        Assert.NotNull(result.Book);
        Assert.NotNull(result.Protection);
        Assert.Empty(result.Diagnostics);
        Assert.True(stream.CanRead);
    }

    [Fact]
    public void RightsMetadataProducesInformationWithoutBlockingReading()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create(includeRights: true);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        EpubDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(EpubDiagnosticCodes.RightsManagementMetadataPresent, diagnostic.Code);
        Assert.Equal(EpubDiagnosticSeverity.Information, diagnostic.Severity);
        Assert.Equal(EpubDiagnosticCategory.Protection, diagnostic.Category);
    }

    [Fact]
    public void StandardFontObfuscationDoesNotBlockCliTextReading()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml(ValidationFixtureFactory.FontPath);
        using MemoryStream stream = ValidationFixtureFactory.Create(
            encryptionXml: encryption,
            includeFontFile: true,
            declareFont: true);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.NotNull(result.Book);
        EpubDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(EpubDiagnosticCodes.FontObfuscationPresent, diagnostic.Code);
        Assert.Equal(EpubDiagnosticSeverity.Information, diagnostic.Severity);
    }

    [Fact]
    public void EncryptedPublicationIsUnsupportedAndNeverDecrypted()
    {
        string encryption = ValidationFixtureFactory.EncryptionXml(
            ValidationFixtureFactory.ChapterPath,
            "http://www.w3.org/2001/04/xmlenc#aes256-cbc");
        using MemoryStream stream = ValidationFixtureFactory.Create(
            encryptionXml: encryption,
            chapterContent: "this deliberately is not XHTML because validation must stop before content parsing");

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Unsupported, result.Status);
        Assert.False(result.CanRead);
        Assert.Null(result.Book);
        Assert.NotNull(result.Protection);
        EpubDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(EpubDiagnosticCodes.UnsupportedEncryption, diagnostic.Code);
        Assert.Equal(EpubDiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void MalformedProtectionMetadataIsInvalidRatherThanUnsupported()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create(encryptionXml: "<encryption>");

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Invalid, result.Status);
        Assert.Null(result.Book);
        Assert.StartsWith("ER-EPUB-PROTECTION-", Assert.Single(result.Diagnostics).Code, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidContainerBecomesStableContainerDiagnostic()
    {
        using MemoryStream stream = new([0x01, 0x02, 0x03]);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Invalid, result.Status);
        EpubDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(EpubDiagnosticCategory.Container, diagnostic.Category);
        Assert.StartsWith("ER-EPUB-CONTAINER-", diagnostic.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidPackageBecomesStablePackageDiagnostic()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create(packageContent: "<package />");

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Invalid, result.Status);
        EpubDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(EpubDiagnosticCategory.Package, diagnostic.Category);
        Assert.StartsWith("ER-EPUB-PACKAGE-", diagnostic.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidNavigationBecomesStableNavigationDiagnostic()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create(
            navigationContent: "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body /></html>");

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Invalid, result.Status);
        EpubDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(EpubDiagnosticCategory.Navigation, diagnostic.Category);
        Assert.StartsWith("ER-EPUB-NAVIGATION-", diagnostic.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void UnsupportedSpineContentBecomesStableContentDiagnostic()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create(chapterMediaType: "application/pdf");

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Invalid, result.Status);
        EpubDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(EpubDiagnosticCategory.Content, diagnostic.Category);
        Assert.StartsWith("ER-EPUB-CONTENT-", diagnostic.Code, StringComparison.Ordinal);
    }


    [Fact]
    public void UnsupportedZipMethodDiscoveredDuringEntryReadBecomesContainerDiagnostic()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create();
        byte[] bytes = stream.ToArray();
        PatchCompressionMethod(bytes, "EPUB/package.opf", 99);
        using MemoryStream corrupted = new(bytes, writable: false);

        EpubValidationResult result = EpubPublicationValidator.Validate(corrupted, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Invalid, result.Status);
        EpubDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(EpubDiagnosticCategory.Container, diagnostic.Category);
        Assert.StartsWith("ER-EPUB-CONTAINER-", diagnostic.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateCanOwnInputStream()
    {
        MemoryStream stream = ValidationFixtureFactory.Create();

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: false);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.False(stream.CanRead);
    }

    [Fact]
    public void DiagnosticsCollectionIsReadOnly()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create(includeRights: true);
        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);
        ICollection<EpubDiagnostic> diagnostics = Assert.IsAssignableFrom<ICollection<EpubDiagnostic>>(result.Diagnostics);

        Assert.Throws<NotSupportedException>(() => diagnostics.Add(new EpubDiagnostic(
            "X",
            EpubDiagnosticSeverity.Information,
            EpubDiagnosticCategory.Container,
            "x")));
    }
    private static void PatchCompressionMethod(byte[] archive, string entryName, ushort method)
    {
        byte[] name = Encoding.UTF8.GetBytes(entryName);
        bool localPatched = false;
        bool centralPatched = false;

        for (int index = 0; index <= archive.Length - name.Length; index++)
        {
            if (!archive.AsSpan(index, name.Length).SequenceEqual(name))
            {
                continue;
            }

            if (index >= 30 &&
                BinaryPrimitives.ReadUInt32LittleEndian(archive.AsSpan(index - 30, 4)) == 0x04034B50)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(archive.AsSpan(index - 22, 2), method);
                localPatched = true;
            }

            if (index >= 46 &&
                BinaryPrimitives.ReadUInt32LittleEndian(archive.AsSpan(index - 46, 4)) == 0x02014B50)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(archive.AsSpan(index - 36, 2), method);
                centralPatched = true;
            }
        }

        Assert.True(localPatched, "Local file header della entry da corrompere non trovato.");
        Assert.True(centralPatched, "Central directory header della entry da corrompere non trovato.");
    }

}
