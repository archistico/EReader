using EbookReader.Epub.Container;

namespace EbookReader.Epub.Tests.Container;

public sealed class OcfPathTests
{
    [Fact]
    public void ArchiveEntryKeepsLiteralPercentEscapes()
    {
        OcfPath path = OcfPath.FromArchiveEntry("EPUB/My%20Book.opf");

        Assert.Equal("EPUB/My%20Book.opf", path.Value);
    }

    [Fact]
    public void ContainerReferenceDecodesPercentEscapes()
    {
        OcfPath path = OcfPath.FromContainerReference("EPUB/My%20Book.opf");

        Assert.Equal("EPUB/My Book.opf", path.Value);
    }

    [Fact]
    public void ContainerReferenceNormalizesInternalDotSegments()
    {
        OcfPath path = OcfPath.FromContainerReference("EPUB/./Text/../package.opf");

        Assert.Equal("EPUB/package.opf", path.Value);
    }

    [Fact]
    public void ContainerReferenceRejectsTraversalAboveRoot()
    {
        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => OcfPath.FromContainerReference("../package.opf"));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }

    [Fact]
    public void ContainerReferenceRejectsAbsolutePath()
    {
        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => OcfPath.FromContainerReference("/EPUB/package.opf"));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }

    [Fact]
    public void ContainerReferenceRejectsBackslash()
    {
        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => OcfPath.FromContainerReference("EPUB\\package.opf"));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }

    [Fact]
    public void ContainerReferenceRejectsEncodedSeparator()
    {
        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => OcfPath.FromContainerReference("EPUB%2Fpackage.opf"));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }

    [Fact]
    public void ContainerReferenceRejectsInvalidPercentEscape()
    {
        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => OcfPath.FromContainerReference("EPUB/%ZZ/package.opf"));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }

    [Fact]
    public void ContainerReferenceRejectsUrlScheme()
    {
        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => OcfPath.FromContainerReference("https:package.opf"));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }

    [Fact]
    public void ArchiveEntryRejectsDriveQualifiedPath()
    {
        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => OcfPath.FromArchiveEntry("C:/EPUB/package.opf"));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }

    [Fact]
    public void ContainerReferenceRejectsDriveQualifiedPath()
    {
        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => OcfPath.FromContainerReference("C:/EPUB/package.opf"));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }

    [Fact]
    public void ContainerReferenceRejectsPercentEncodedDriveQualifiedPath()
    {
        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => OcfPath.FromContainerReference("C%3A/EPUB/package.opf"));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }

    [Fact]
    public void ArchiveEntryRejectsTraversalSegment()
    {
        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => OcfPath.FromArchiveEntry("EPUB/../package.opf"));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }
}
