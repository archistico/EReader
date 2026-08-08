namespace EbookReader.Epub.Resources;

public enum EpubImageResourceErrorCode
{
    ResourceNotFound = 1,
    ResourceIsRemote = 2,
    ResourceIsNotImage = 3,
    UnsupportedImageMediaType = 4,
    ResourceTooLarge = 5,
}
