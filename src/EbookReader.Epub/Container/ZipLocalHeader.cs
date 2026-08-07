using System.Buffers.Binary;
using System.Text;

namespace EbookReader.Epub.Container;

internal sealed record ZipLocalHeader(
    string FileName,
    ushort VersionNeeded,
    ushort GeneralPurposeFlags,
    ushort CompressionMethod,
    ushort ExtraFieldLength)
{
    private const uint LocalFileHeaderSignature = 0x04034B50;
    private const int FixedHeaderLength = 30;

    public bool UsesEncryption => (GeneralPurposeFlags & 0x0001) != 0;

    public static ZipLocalHeader ReadFirst(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("Lo stream EPUB deve essere leggibile e seekable.", nameof(stream));
        }

        stream.Position = 0;
        Span<byte> header = stackalloc byte[FixedHeaderLength];
        ReadExactlyOrThrow(stream, header);

        if (BinaryPrimitives.ReadUInt32LittleEndian(header) != LocalFileHeaderSignature)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.InvalidZip,
                "Il file non inizia con un local file header ZIP valido.");
        }

        ushort versionNeeded = BinaryPrimitives.ReadUInt16LittleEndian(header[4..6]);
        ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(header[6..8]);
        ushort compression = BinaryPrimitives.ReadUInt16LittleEndian(header[8..10]);
        ushort fileNameLength = BinaryPrimitives.ReadUInt16LittleEndian(header[26..28]);
        ushort extraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(header[28..30]);

        if (fileNameLength == 0)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.InvalidZip,
                "La prima entry ZIP non contiene un nome file.");
        }

        byte[] fileNameBytes = new byte[fileNameLength];
        ReadExactlyOrThrow(stream, fileNameBytes);

        string fileName;
        try
        {
            fileName = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(fileNameBytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.InvalidZip,
                "Il nome della prima entry ZIP non è UTF-8 valido.",
                exception);
        }

        stream.Position = 0;
        return new ZipLocalHeader(fileName, versionNeeded, flags, compression, extraFieldLength);
    }

    private static void ReadExactlyOrThrow(Stream stream, Span<byte> buffer)
    {
        try
        {
            stream.ReadExactly(buffer);
        }
        catch (EndOfStreamException exception)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.InvalidZip,
                "Archivio ZIP troncato durante la lettura del local file header.",
                exception);
        }
    }
}
