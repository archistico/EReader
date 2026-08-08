using EbookReader.Epub.Validation;

namespace EbookReader.Epub.Tests.Validation;

public sealed class ReliabilityCorpusTests
{
    [Theory]
    [InlineData("valid-minimal.epub", EpubValidationStatus.Valid, "")]
    [InlineData("missing-navigation.epub", EpubValidationStatus.Valid, EpubDiagnosticCodes.NavigationUnavailable)]
    [InlineData("missing-image.epub", EpubValidationStatus.Valid, EpubDiagnosticCodes.MissingReferencedImage)]
    [InlineData("broken-internal-link.epub", EpubValidationStatus.Valid, EpubDiagnosticCodes.BrokenInternalHyperlink)]
    [InlineData("unsafe-external-link.epub", EpubValidationStatus.Valid, EpubDiagnosticCodes.UnsafeExternalHyperlinkSuppressed)]
    [InlineData("missing-container.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTAINER-011")]
    [InlineData("malformed-container.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTAINER-013")]
    [InlineData("missing-opf.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTAINER-019")]
    [InlineData("malformed-opf.epub", EpubValidationStatus.Invalid, "ER-EPUB-PACKAGE-002")]
    [InlineData("missing-primary-chapter.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTENT-003")]
    [InlineData("malformed-primary-xhtml.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTENT-004")]
    [InlineData("invalid-utf8-primary.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTENT-004")]
    [InlineData("unsupported-encryption.epub", EpubValidationStatus.Unsupported, EpubDiagnosticCodes.UnsupportedEncryption)]
    [InlineData("duplicate-entry.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTAINER-010")]
    [InlineData("path-traversal-entry.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTAINER-009")]
    [InlineData("drive-prefixed-entry.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTAINER-009")]
    [InlineData("symlink-entry.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTAINER-024")]
    [InlineData("compression-ratio-bomb.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTAINER-023")]
    [InlineData("oversized-entry-metadata.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTAINER-021")]
    [InlineData("truncated-zip.epub", EpubValidationStatus.Invalid, "ER-EPUB-CONTAINER-001")]
    public void CorpusFixtureHasExpectedDeterministicOutcome(
        string fileName,
        EpubValidationStatus expectedStatus,
        string expectedDiagnosticCode)
    {
        string path = CorpusPath(fileName);

        EpubValidationResult result = EpubPublicationValidator.Validate(path);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedStatus == EpubValidationStatus.Valid, result.CanRead);

        if (expectedDiagnosticCode.Length == 0)
        {
            Assert.Empty(result.Diagnostics);
        }
        else
        {
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == expectedDiagnosticCode);
        }
    }

    [Fact]
    public void CorpusContainsExactlyTheDocumentedTwentyEpubFixtures()
    {
        string directory = CorpusDirectory();

        string[] fixtures = Directory.GetFiles(directory, "*.epub", SearchOption.TopDirectoryOnly);

        Assert.Equal(20, fixtures.Length);
        Assert.True(File.Exists(Path.Combine(directory, "README.md")));
    }

    [Theory]
    [InlineData("path-traversal-entry.epub")]
    [InlineData("symlink-entry.epub")]
    [InlineData("compression-ratio-bomb.epub")]
    [InlineData("oversized-entry-metadata.epub")]
    public void SecurityCorpusValidationNeverMutatesSourceArchive(string fileName)
    {
        string path = CorpusPath(fileName);
        byte[] before = File.ReadAllBytes(path);

        EpubValidationResult result = EpubPublicationValidator.Validate(path);
        byte[] after = File.ReadAllBytes(path);

        Assert.Equal(EpubValidationStatus.Invalid, result.Status);
        Assert.Equal(before, after);
    }

    private static string CorpusPath(string fileName) =>
        Path.Combine(CorpusDirectory(), fileName);

    private static string CorpusDirectory() =>
        Path.Combine(AppContext.BaseDirectory, "reliability");
}
