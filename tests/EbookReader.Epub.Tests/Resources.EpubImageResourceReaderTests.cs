using System.Text;
using EbookReader.Domain.Resources;
using EbookReader.Epub.Resources;
using EbookReader.Epub.Tests.Package;

namespace EbookReader.Epub.Tests;

public sealed class EpubImageResourceReaderTests
{
    [Fact]
    public void ReadsLocalJpegByManifestResourceId()
    {
        using MemoryStream epub = OpfFixtureFactory.CreateEpub3();
        string path = WriteTemporaryEpub(epub);
        try
        {
            EpubImageResource resource = EpubImageResourceReader.Read(path, new ResourceId("cover"));

            Assert.Equal("image/jpeg", resource.MediaType);
            Assert.Equal(".jpg", resource.FileExtension);
            Assert.Equal("not-a-real-jpeg", Encoding.UTF8.GetString(resource.Data.Span));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RejectsManifestResourceThatIsNotAnImage()
    {
        using MemoryStream epub = OpfFixtureFactory.CreateEpub3();
        string path = WriteTemporaryEpub(epub);
        try
        {
            EpubImageResourceException exception = Assert.Throws<EpubImageResourceException>(
                () => EpubImageResourceReader.Read(path, new ResourceId("c1")));

            Assert.Equal(EpubImageResourceErrorCode.ResourceIsNotImage, exception.ErrorCode);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RejectsSvgForExternalPreview()
    {
        const string manifest = "<item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" /><item id=\"vector\" href=\"images/vector.svg\" media-type=\"image/svg+xml\" />";
        (string Path, string Content)[] entries =
        [
            ("EPUB/nav.xhtml", "<html />"),
            ("EPUB/Text/ch1.xhtml", "<html />"),
            ("EPUB/images/vector.svg", "<svg xmlns=\"http://www.w3.org/2000/svg\" />"),
        ];
        using MemoryStream epub = OpfFixtureFactory.CreateEpub3(
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />",
            additionalEntries: entries);
        string path = WriteTemporaryEpub(epub);
        try
        {
            EpubImageResourceException exception = Assert.Throws<EpubImageResourceException>(
                () => EpubImageResourceReader.Read(path, new ResourceId("vector")));

            Assert.Equal(EpubImageResourceErrorCode.UnsupportedImageMediaType, exception.ErrorCode);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RejectsRemoteImageResource()
    {
        const string manifest = "<item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" /><item id=\"remote\" href=\"https://example.invalid/image.png\" media-type=\"image/png\" />";
        (string Path, string Content)[] entries =
        [
            ("EPUB/nav.xhtml", "<html />"),
            ("EPUB/Text/ch1.xhtml", "<html />"),
        ];
        using MemoryStream epub = OpfFixtureFactory.CreateEpub3(
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />",
            additionalEntries: entries);
        string path = WriteTemporaryEpub(epub);
        try
        {
            EpubImageResourceException exception = Assert.Throws<EpubImageResourceException>(
                () => EpubImageResourceReader.Read(path, new ResourceId("remote")));

            Assert.Equal(EpubImageResourceErrorCode.ResourceIsRemote, exception.ErrorCode);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RejectsUnknownResourceId()
    {
        using MemoryStream epub = OpfFixtureFactory.CreateEpub3();
        string path = WriteTemporaryEpub(epub);
        try
        {
            EpubImageResourceException exception = Assert.Throws<EpubImageResourceException>(
                () => EpubImageResourceReader.Read(path, new ResourceId("missing")));

            Assert.Equal(EpubImageResourceErrorCode.ResourceNotFound, exception.ErrorCode);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteTemporaryEpub(MemoryStream source)
    {
        string path = Path.Combine(Path.GetTempPath(), $"ereader-image-{Guid.NewGuid():N}.epub");
        source.Position = 0;
        using FileStream destination = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        source.CopyTo(destination);
        return path;
    }
}
