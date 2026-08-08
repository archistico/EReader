using EbookReader.Cli.Tui;

namespace EbookReader.Cli.Images;

internal interface IReaderImagePreviewService
{
    ImagePreviewResult Open(ReaderImageInfo image);
}
