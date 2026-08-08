using System.Buffers.Binary;
using System.Text;
using EbookReader.Domain.Content;
using EbookReader.Domain.Navigation;
using EbookReader.Epub.Validation;
using EbookReader.Epub.Tests.Package;

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
    public void InvalidNavigationDegradesToReadableBookWithoutToc()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create(
            navigationContent: "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body /></html>");

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.True(result.CanRead);
        Assert.NotNull(result.Book);
        Assert.Empty(result.Book.TableOfContents.Items);
        EpubDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(EpubDiagnosticCodes.NavigationUnavailable, diagnostic.Code);
        Assert.Equal(EpubDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(EpubDiagnosticCategory.Navigation, diagnostic.Category);
    }


    [Fact]
    public void UnresolvableLeafNavigationTargetDropsOnlyBrokenLeafAndKeepsOtherTocStructure()
    {
        const string navigation = """
        <?xml version="1.0" encoding="UTF-8"?>
        <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
          <head><title>Navigation</title></head>
          <body><nav epub:type="toc"><ol>
            <li><a href="Text/ch1.xhtml#missing-anchor">Broken target</a></li>
            <li><a href="Text/ch1.xhtml#start">Valid target</a></li>
          </ol></nav></body>
        </html>
        """;
        using MemoryStream stream = ValidationFixtureFactory.Create(navigationContent: navigation);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.NotNull(result.Book);
        NavigationItem item = Assert.Single(result.Book.TableOfContents.Items);
        Assert.Equal("Valid target", item.Label);
        Assert.NotNull(item.Target);
        EpubDiagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Code == EpubDiagnosticCodes.NavigationTargetDropped);
        Assert.Equal(EpubDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(EpubDiagnosticCategory.Navigation, diagnostic.Category);
    }

    [Fact]
    public void UnresolvableNavigationParentTargetKeepsGroupingNodeWhenChildIsValid()
    {
        const string navigation = """
        <?xml version="1.0" encoding="UTF-8"?>
        <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
          <head><title>Navigation</title></head>
          <body><nav epub:type="toc"><ol>
            <li><a href="Text/ch1.xhtml#missing-anchor">Broken parent</a><ol>
              <li><a href="Text/ch1.xhtml#start">Valid child</a></li>
            </ol></li>
          </ol></nav></body>
        </html>
        """;
        using MemoryStream stream = ValidationFixtureFactory.Create(navigationContent: navigation);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.NotNull(result.Book);
        NavigationItem parent = Assert.Single(result.Book.TableOfContents.Items);
        Assert.Equal("Broken parent", parent.Label);
        Assert.Null(parent.Target);
        NavigationItem child = Assert.Single(parent.Children);
        Assert.Equal("Valid child", child.Label);
        Assert.NotNull(child.Target);
        Assert.Single(
            result.Diagnostics,
            candidate => candidate.Code == EpubDiagnosticCodes.NavigationTargetDropped);
    }

    [Fact]
    public void GroupingNavigationNodeIsDroppedWhenAllRecoveredChildrenDisappear()
    {
        const string navigation = """
        <?xml version="1.0" encoding="UTF-8"?>
        <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
          <head><title>Navigation</title></head>
          <body><nav epub:type="toc"><ol>
            <li><span>Group</span><ol>
              <li><a href="Text/ch1.xhtml#missing-anchor">Broken child</a></li>
            </ol></li>
          </ol></nav></body>
        </html>
        """;
        using MemoryStream stream = ValidationFixtureFactory.Create(navigationContent: navigation);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.NotNull(result.Book);
        Assert.Empty(result.Book.TableOfContents.Items);
        Assert.Single(
            result.Diagnostics,
            candidate => candidate.Code == EpubDiagnosticCodes.NavigationTargetDropped);
    }

    [Fact]
    public void BrokenInternalAnchorKeepsBookReadableAndPreservesLinkText()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1><p><a href="#missing">Broken link text</a></p>
        </body></html>
        """;
        using MemoryStream stream = ValidationFixtureFactory.Create(chapterContent: chapter);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.NotNull(result.Book);
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(result.Book.ReadingOrder[0].Blocks[1]);
        Assert.Equal("Broken link text", ContentText.GetPlainText(paragraph));
        Assert.DoesNotContain(paragraph.Content, item => item is HyperlinkSpan);
        EpubDiagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Code == EpubDiagnosticCodes.BrokenInternalHyperlink);
        Assert.Equal(EpubDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(EpubDiagnosticCategory.Navigation, diagnostic.Category);
    }

    [Fact]
    public void BrokenNoteReferenceIsDiagnosticButDoesNotMakePublicationUnreadable()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops"><body>
          <h1 id="start">One</h1><p>Text<a epub:type="noteref" href="#missing-note">1</a></p>
        </body></html>
        """;
        using MemoryStream stream = ValidationFixtureFactory.Create(chapterContent: chapter);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        EpubDiagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Code == EpubDiagnosticCodes.BrokenInternalHyperlink);
        Assert.Contains("Rimando nota", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LinkToNonReadingResourceIsNonActionableWithDiagnostic()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1><p><a href="../styles/book.css">Stylesheet</a></p>
        </body></html>
        """;
        string manifest = """
        <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav" />
        <item id="c1" href="Text/ch1.xhtml" media-type="application/xhtml+xml" />
        <item id="css" href="styles/book.css" media-type="text/css" />
        """;
        string package = OpfFixtureFactory.CreateEpub3Package(
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />");
        using MemoryStream stream = ValidationFixtureFactory.Create(
            chapterContent: chapter,
            packageContent: package,
            additionalEntries: [("EPUB/styles/book.css", "body { color: black; }")]);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        EpubDiagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Code == EpubDiagnosticCodes.BrokenInternalHyperlink);
        Assert.Contains("non appartiene al reading order navigabile", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PercentEncodedTraversalLinkIsSuppressedWithoutEscapingPackage()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1><p><a href="%2E%2E/%2E%2E/%2E%2E/outside.xhtml">Outside</a></p>
        </body></html>
        """;
        using MemoryStream stream = ValidationFixtureFactory.Create(chapterContent: chapter);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.NotNull(result.Book);
        EpubDiagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Code == EpubDiagnosticCodes.BrokenInternalHyperlink);
        Assert.Contains("Riferimento locale non valido", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnsafeExternalSchemeIsSuppressedAndReportedWithoutNetworkOrShellHandoff()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1><p><a href="javascript:alert(1)">Do not execute</a></p>
        </body></html>
        """;
        using MemoryStream stream = ValidationFixtureFactory.Create(chapterContent: chapter);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.NotNull(result.Book);
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(result.Book.ReadingOrder[0].Blocks[1]);
        Assert.DoesNotContain(paragraph.Content, item => item is HyperlinkSpan);
        EpubDiagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Code == EpubDiagnosticCodes.UnsafeExternalHyperlinkSuppressed);
        Assert.Equal(EpubDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("non viene delegato al sistema operativo", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingNavigationFileDegradesToReadableBookWithoutToc()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create(includeNavigationFile: false);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.NotNull(result.Book);
        Assert.Empty(result.Book.TableOfContents.Items);
        EpubDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(EpubDiagnosticCodes.NavigationUnavailable, diagnostic.Code);
        Assert.Equal(EpubDiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void MissingPrimarySpineContentRemainsFatal()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create(
            includeNavigationFile: false,
            includeChapterFile: false);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Invalid, result.Status);
        Assert.False(result.CanRead);
        Assert.Null(result.Book);
        EpubDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(EpubDiagnosticCategory.Content, diagnostic.Category);
        Assert.StartsWith("ER-EPUB-CONTENT-", diagnostic.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingSupplementarySpineContentIsSkippedDeterministically()
    {
        string manifest = """
        <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav" />
        <item id="c1" href="Text/ch1.xhtml" media-type="application/xhtml+xml" />
        <item id="c2" href="Text/ch2.xhtml" media-type="application/xhtml+xml" />
        """;
        string package = OpfFixtureFactory.CreateEpub3Package(
            manifest: manifest,
            spine: "<itemref idref=\"c1\" /><itemref idref=\"c2\" linear=\"no\" />");
        using MemoryStream stream = ValidationFixtureFactory.Create(packageContent: package);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.NotNull(result.Book);
        Assert.Single(result.Book.ReadingOrder);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == EpubDiagnosticCodes.SupplementarySpineItemSkipped &&
                          diagnostic.Severity == EpubDiagnosticSeverity.Error);
    }

    [Fact]
    public void MissingReferencedImageKeepsTextReadableWithPlaceholderDiagnostic()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1>
          <img src="../images/missing.jpg" alt="Mappa assente" />
        </body></html>
        """;
        string manifest = """
        <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav" />
        <item id="c1" href="Text/ch1.xhtml" media-type="application/xhtml+xml" />
        <item id="img" href="images/missing.jpg" media-type="image/jpeg" />
        """;
        string package = OpfFixtureFactory.CreateEpub3Package(
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />");
        using MemoryStream stream = ValidationFixtureFactory.Create(
            packageContent: package,
            chapterContent: chapter);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        Assert.NotNull(result.Book);
        Assert.Contains(
            result.Book.ReadingOrder.SelectMany(section => section.Blocks).OfType<EbookReader.Domain.Content.ImageBlock>(),
            image => image.AlternativeText == "Mappa assente");
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == EpubDiagnosticCodes.MissingReferencedImage &&
                          diagnostic.Severity == EpubDiagnosticSeverity.Error);
    }

    [Fact]
    public void MissingOptionalCssProducesWarningWithoutBlockingReading()
    {
        string manifest = """
        <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav" />
        <item id="c1" href="Text/ch1.xhtml" media-type="application/xhtml+xml" />
        <item id="css" href="styles/missing.css" media-type="text/css" />
        """;
        string package = OpfFixtureFactory.CreateEpub3Package(
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />");
        using MemoryStream stream = ValidationFixtureFactory.Create(packageContent: package);

        EpubValidationResult result = EpubPublicationValidator.Validate(stream, leaveOpen: true);

        Assert.Equal(EpubValidationStatus.Valid, result.Status);
        EpubDiagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Code == EpubDiagnosticCodes.MissingOptionalResource);
        Assert.Equal(EpubDiagnosticSeverity.Warning, diagnostic.Severity);
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
    public void UnsafeNavigationEntryFailureRemainsFatalContainerError()
    {
        using MemoryStream stream = ValidationFixtureFactory.Create();
        byte[] bytes = stream.ToArray();
        PatchCompressionMethod(bytes, "EPUB/nav.xhtml", 99);
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
