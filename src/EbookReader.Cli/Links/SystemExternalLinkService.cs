using System.ComponentModel;
using System.Diagnostics;
using EbookReader.Domain.Content;

namespace EbookReader.Cli.Links;

/// <summary>
/// Explicit OS-shell adapter for external Domain links. The EPUB parser already filters schemes;
/// this boundary repeats the allow-list before delegating to the operating system.
/// </summary>
internal sealed class SystemExternalLinkService : IReaderExternalLinkService
{
    public ExternalLinkOpenResult Open(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!ExternalLinkPolicy.IsAllowed(uri))
        {
            return ExternalLinkOpenResult.Failure("Schema link esterno non supportato.");
        }

        try
        {
            ProcessStartInfo startInfo = new(uri.AbsoluteUri)
            {
                UseShellExecute = true,
            };
            using Process? process = Process.Start(startInfo);
            return process is null
                ? ExternalLinkOpenResult.Failure("Impossibile avviare il browser/applicazione di sistema.")
                : ExternalLinkOpenResult.Success();
        }
        catch (Win32Exception exception)
        {
            return ExternalLinkOpenResult.Failure($"Browser/applicazione di sistema non disponibile: {exception.Message}");
        }
        catch (IOException exception)
        {
            return ExternalLinkOpenResult.Failure($"Errore di I/O durante l'apertura del link esterno: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return ExternalLinkOpenResult.Failure($"Accesso negato durante l'apertura del link esterno: {exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            return ExternalLinkOpenResult.Failure($"Impossibile aprire il link esterno: {exception.Message}");
        }
        catch (PlatformNotSupportedException exception)
        {
            return ExternalLinkOpenResult.Failure($"Apertura link esterni non supportata: {exception.Message}");
        }
    }

}
