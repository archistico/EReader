using System.ComponentModel;
using System.Diagnostics;
using EbookReader.Cli.Tui;
using EbookReader.Epub.Container;
using EbookReader.Epub.Package;
using EbookReader.Epub.Resources;

namespace EbookReader.Cli.Images;

/// <summary>
/// Explicit outer-adapter bridge from an EPUB raster resource to the operating-system viewer.
/// One bounded resource is copied to a private temporary directory only after the user presses Enter.
/// </summary>
internal sealed class ExternalImagePreviewService : IReaderImagePreviewService, IDisposable
{
    private readonly string _epubFilePath;
    private string? _temporaryDirectory;
    private int _nextFileNumber;
    private bool _disposed;

    public ExternalImagePreviewService(string epubFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(epubFilePath);
        _epubFilePath = Path.GetFullPath(epubFilePath);
    }

    public ImagePreviewResult Open(ReaderImageInfo image)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(image);

        try
        {
            EpubImageResource resource = EpubImageResourceReader.Read(_epubFilePath, image.ResourceId);
            string directory = EnsureTemporaryDirectory();
            _nextFileNumber++;
            string temporaryPath = Path.Combine(
                directory,
                $"image-{_nextFileNumber:D4}{resource.FileExtension}");

            using (FileStream stream = new(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.Read,
                       bufferSize: 16 * 1024,
                       FileOptions.WriteThrough))
            {
                stream.Write(resource.Data.Span);
                stream.Flush(flushToDisk: true);
            }

            ProcessStartInfo startInfo = new(temporaryPath)
            {
                UseShellExecute = true,
            };
            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                TryDeleteFile(temporaryPath);
                return ImagePreviewResult.Failure("Impossibile avviare il viewer di sistema per l'immagine.");
            }

            return ImagePreviewResult.Success();
        }
        catch (EpubImageResourceException exception)
        {
            return ImagePreviewResult.Failure(exception.Message);
        }
        catch (EpubContainerException exception)
        {
            return ImagePreviewResult.Failure($"Impossibile rileggere la risorsa EPUB: {exception.Message}");
        }
        catch (EpubPackageException exception)
        {
            return ImagePreviewResult.Failure($"Impossibile rileggere il manifest EPUB: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return ImagePreviewResult.Failure($"Accesso negato durante l'anteprima immagine: {exception.Message}");
        }
        catch (IOException exception)
        {
            return ImagePreviewResult.Failure($"Errore di I/O durante l'anteprima immagine: {exception.Message}");
        }
        catch (Win32Exception exception)
        {
            return ImagePreviewResult.Failure($"Viewer di sistema non disponibile: {exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            return ImagePreviewResult.Failure($"Impossibile avviare il viewer di sistema: {exception.Message}");
        }
        catch (PlatformNotSupportedException exception)
        {
            return ImagePreviewResult.Failure($"Anteprima esterna non supportata su questa piattaforma: {exception.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_temporaryDirectory is not null)
        {
            TryDeleteDirectory(_temporaryDirectory);
        }

        GC.SuppressFinalize(this);
    }

    private string EnsureTemporaryDirectory()
    {
        if (_temporaryDirectory is not null)
        {
            return _temporaryDirectory;
        }

        string parent = Path.Combine(Path.GetTempPath(), "EReader", "ImagePreview");
        string directory = Path.Combine(parent, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        _temporaryDirectory = directory;
        return directory;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // The system viewer may already own the file; best-effort cleanup is intentional.
        }
        catch (UnauthorizedAccessException)
        {
            // The preview result must not be hidden by cleanup failure.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // A viewer may still hold the resource. The OS temp directory remains the fallback cleanup boundary.
        }
        catch (UnauthorizedAccessException)
        {
            // Same rationale: never turn successful reading into a fatal cleanup failure.
        }
    }
}
