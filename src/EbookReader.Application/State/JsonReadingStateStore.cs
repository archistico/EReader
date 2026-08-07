using System.Text.Json;
using System.Text.Json.Serialization;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.State;

/// <summary>
/// Versioned single-book JSON persistence. Writes use a temporary file in the same directory,
/// flush it to disk, then replace the destination with a same-volume rename.
/// </summary>
public sealed class JsonReadingStateStore
{
    public const int CurrentSchemaVersion = 1;
    public const long MaximumStateBytes = 1_048_576;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public JsonReadingStateStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public ReadingStateSnapshot? Load()
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }

        FileInfo info = new(FilePath);
        if (info.Length > MaximumStateBytes)
        {
            throw new InvalidDataException(
                $"Il file di stato supera il limite di {MaximumStateBytes} byte.");
        }

        try
        {
            using FileStream stream = new(
                FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.SequentialScan);

            StateDocumentDto? document = JsonSerializer.Deserialize<StateDocumentDto>(stream, SerializerOptions);
            if (document is null)
            {
                throw new InvalidDataException("Il file di stato JSON è vuoto.");
            }

            return FromDocument(document);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Il file di stato JSON non è valido.", exception);
        }
    }

    public void Save(ReadingStateSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);

        string? directory = Path.GetDirectoryName(FilePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Il percorso del file di stato non ha una directory valida.");
        }

        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (FileStream stream = new(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, ToDocument(state), SerializerOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        finally
        {
            DeleteTemporaryFileIfPresent(temporaryPath);
        }
    }

    private static ReadingStateSnapshot FromDocument(StateDocumentDto document)
    {
        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Versione schema stato non supportata: {document.SchemaVersion}.");
        }

        LastBookDto lastBook = document.LastBook
            ?? throw new InvalidDataException("Il documento di stato non contiene lastBook.");
        LocationDto location = lastBook.Location
            ?? throw new InvalidDataException("Il documento di stato non contiene location.");

        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(lastBook.Path);
            ArgumentException.ThrowIfNullOrWhiteSpace(lastBook.BookId);
            ArgumentException.ThrowIfNullOrWhiteSpace(location.SectionId);
            if (lastBook.LastOpenedUtc == default)
            {
                throw new InvalidDataException("Il documento di stato non contiene un lastOpenedUtc valido.");
            }

            SectionId sectionId = new(location.SectionId);
            BlockId? blockId = string.IsNullOrWhiteSpace(location.BlockId)
                ? null
                : new BlockId(location.BlockId);
            ReadingLocation readingLocation = new(sectionId, blockId, location.CharacterOffset);

            return new ReadingStateSnapshot(
                lastBook.Path,
                new BookId(lastBook.BookId),
                readingLocation,
                lastBook.LastOpenedUtc);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("Il documento di stato contiene valori non validi.", exception);
        }
    }

    private static StateDocumentDto ToDocument(ReadingStateSnapshot state) =>
        new()
        {
            SchemaVersion = CurrentSchemaVersion,
            LastBook = new LastBookDto
            {
                Path = state.BookPath,
                BookId = state.BookId.Value,
                LastOpenedUtc = state.LastOpenedUtc,
                Location = new LocationDto
                {
                    SectionId = state.Location.SectionId.Value,
                    BlockId = state.Location.BlockId?.Value,
                    CharacterOffset = state.Location.CharacterOffset,
                },
            },
        };

    private static void DeleteTemporaryFileIfPresent(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // A stale temp file is preferable to hiding the original save result.
        }
        catch (UnauthorizedAccessException)
        {
            // Same rationale as above; the destination state is never deleted here.
        }
    }

    private sealed class StateDocumentDto
    {
        public int SchemaVersion { get; init; }

        public LastBookDto? LastBook { get; init; }
    }

    private sealed class LastBookDto
    {
        public string? Path { get; init; }

        public string? BookId { get; init; }

        public DateTimeOffset LastOpenedUtc { get; init; }

        public LocationDto? Location { get; init; }
    }

    private sealed class LocationDto
    {
        public string? SectionId { get; init; }

        public string? BlockId { get; init; }

        public int CharacterOffset { get; init; }
    }
}
