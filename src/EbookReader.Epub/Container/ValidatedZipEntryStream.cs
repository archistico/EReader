namespace EbookReader.Epub.Container;

/// <summary>
/// Read-only guard around a ZipArchiveEntry stream. It keeps archive corruption and
/// inconsistent central-directory metadata inside the EPUB boundary instead of leaking
/// framework exceptions to higher layers.
/// </summary>
internal sealed class ValidatedZipEntryStream : Stream
{
    private readonly Stream _inner;
    private readonly string _entryPath;
    private readonly long _expectedLength;
    private long _bytesRead;
    private bool _disposed;

    public ValidatedZipEntryStream(Stream inner, string entryPath, long expectedLength)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryPath);

        if (!inner.CanRead)
        {
            inner.Dispose();
            throw new EpubContainerException(
                EpubContainerErrorCode.InconsistentArchiveEntry,
                $"La entry ZIP '{entryPath}' non espone uno stream leggibile.");
        }

        if (expectedLength < 0 || expectedLength > EpubContainerLimits.MaxEntryUncompressedBytes)
        {
            inner.Dispose();
            throw new EpubContainerException(
                EpubContainerErrorCode.ArchiveEntryTooLarge,
                $"La entry ZIP '{entryPath}' dichiara una dimensione decompressa non ammessa: {expectedLength} byte.");
        }

        _inner = inner;
        _entryPath = entryPath;
        _expectedLength = expectedLength;
    }

    public override bool CanRead => !_disposed && _inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _expectedLength;

    public override long Position
    {
        get => _bytesRead;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            return ValidateRead(_inner.Read(buffer, offset, count));
        }
        catch (InvalidDataException exception)
        {
            throw InvalidZip(exception);
        }
        catch (EndOfStreamException exception)
        {
            throw InvalidZip(exception);
        }
        catch (NotSupportedException exception)
        {
            throw UnsupportedZip(exception);
        }
    }

    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            return ValidateRead(_inner.Read(buffer));
        }
        catch (InvalidDataException exception)
        {
            throw InvalidZip(exception);
        }
        catch (EndOfStreamException exception)
        {
            throw InvalidZip(exception);
        }
        catch (NotSupportedException exception)
        {
            throw UnsupportedZip(exception);
        }
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _inner.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    private int ValidateRead(int read)
    {
        if (read == 0)
        {
            if (_bytesRead != _expectedLength)
            {
                throw new EpubContainerException(
                    EpubContainerErrorCode.InconsistentArchiveEntry,
                    $"La entry ZIP '{_entryPath}' è terminata dopo {_bytesRead} byte, ma la directory centrale ne dichiara {_expectedLength}.");
            }

            return 0;
        }

        _bytesRead += read;
        if (_bytesRead > _expectedLength || _bytesRead > EpubContainerLimits.MaxEntryUncompressedBytes)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.InconsistentArchiveEntry,
                $"La entry ZIP '{_entryPath}' produce più dati decompressi di quanto dichiarato o consentito.");
        }

        return read;
    }

    private EpubContainerException InvalidZip(Exception exception) =>
        new(
            EpubContainerErrorCode.InconsistentArchiveEntry,
            $"La entry ZIP '{_entryPath}' è corrotta o incoerente con la directory centrale.",
            exception);

    private EpubContainerException UnsupportedZip(Exception exception) =>
        new(
            EpubContainerErrorCode.UnsupportedZipFeature,
            $"La entry ZIP '{_entryPath}' usa una funzione o un metodo di compressione non supportato.",
            exception);
}
