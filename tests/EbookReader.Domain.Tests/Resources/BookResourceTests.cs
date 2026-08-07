namespace EbookReader.Domain.Tests.Resources;

public sealed class BookResourceTests
{
    [Fact]
    public void ResourceRequiresMediaType()
    {
        Assert.Throws<ArgumentException>(
            () => new BookResource(new ResourceId("r1"), ResourceKind.Image, " "));
    }

    [Fact]
    public void ResourceRejectsNegativeByteLength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BookResource(new ResourceId("r1"), ResourceKind.Image, "image/png", byteLength: -1));
    }

    [Fact]
    public void ResourceRejectsUndefinedKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BookResource(new ResourceId("r1"), (ResourceKind)999, "application/octet-stream"));
    }

    [Fact]
    public void ResourceCarriesDescriptorWithoutPayload()
    {
        BookResource resource = new(new ResourceId("cover"), ResourceKind.Image, "image/jpeg", "cover.jpg", 1234);

        Assert.Equal(ResourceKind.Image, resource.Kind);
        Assert.Equal(1234L, resource.ByteLength);
    }
}
