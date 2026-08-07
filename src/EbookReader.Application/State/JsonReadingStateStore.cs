using System.Text.Json;
using System.Text.Json.Serialization;
using EbookReader.Application.Library;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.State;

/// <summary>
/// Versioned JSON persistence for resume state, logical bookmarks and bounded recent-book history. Writes use a temporary file in the same
/// directory, flush it to disk, then replace the destination with a same-volume rename.
/// </summary>
public sealed class JsonReadingStateStore
{
    public const int CurrentSchemaVersion = 3;
    public const int MaximumBookmarks = 10_000;
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

            FileInfo temporaryInfo = new(temporaryPath);
            if (temporaryInfo.Length > MaximumStateBytes)
            {
                throw new InvalidOperationException(
                    $"Lo stato serializzato supera il limite di {MaximumStateBytes} byte.");
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
        if (document.SchemaVersion is not (1 or 2 or CurrentSchemaVersion))
        {
            throw new InvalidDataException(
                $"Versione schema stato non supportata: {document.SchemaVersion}.");
        }

        LastBookDto lastBook = document.LastBook
            ?? throw new InvalidDataException("Il documento di stato non contiene lastBook.");
        LocationDto location = lastBook.Location
            ?? throw new InvalidDataException("Il documento di stato non contiene location.");

        if (document.Bookmarks is { Count: > MaximumBookmarks })
        {
            throw new InvalidDataException(
                $"Il documento di stato contiene più di {MaximumBookmarks} bookmark.");
        }

        if (document.History is { Count: > ReadingHistoryState.MaximumEntries })
        {
            throw new InvalidDataException(
                $"Il documento di stato contiene più di {ReadingHistoryState.MaximumEntries} voci di cronologia.");
        }

        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(lastBook.Path);
            ArgumentException.ThrowIfNullOrWhiteSpace(lastBook.BookId);
            if (lastBook.LastOpenedUtc == default)
            {
                throw new InvalidDataException("Il documento di stato non contiene un lastOpenedUtc valido.");
            }

            ReadingLocation readingLocation = CreateLocation(location);
            List<ReadingBookmarkSnapshot> bookmarks = [];
            List<ReadingHistoryEntry> history = [];

            if (document.SchemaVersion >= 2 && document.Bookmarks is not null)
            {
                foreach (BookmarkDto bookmark in document.Bookmarks)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(bookmark.Path);
                    ArgumentException.ThrowIfNullOrWhiteSpace(bookmark.BookId);
                    LocationDto bookmarkLocation = bookmark.Location
                        ?? throw new InvalidDataException("Un bookmark non contiene location.");
                    ReadingBookmarkSnapshot snapshot = new(
                        bookmark.Path,
                        new BookId(bookmark.BookId),
                        CreateLocation(bookmarkLocation));

                    if (bookmarks.Contains(snapshot))
                    {
                        throw new InvalidDataException("Il documento di stato contiene bookmark duplicati.");
                    }

                    bookmarks.Add(snapshot);
                }
            }

            if (document.SchemaVersion >= 3 && document.History is not null)
            {
                foreach (HistoryDto item in document.History)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(item.Path);
                    ArgumentException.ThrowIfNullOrWhiteSpace(item.BookId);
                    ArgumentException.ThrowIfNullOrWhiteSpace(item.Title);
                    if (item.LastOpenedUtc == default)
                    {
                        throw new InvalidDataException("Una voce di cronologia non contiene lastOpenedUtc valido.");
                    }
                    LocationDto historyLocation = item.Location
                        ?? throw new InvalidDataException("Una voce di cronologia non contiene location.");
                    ReadingHistoryEntry historyEntry = new(
                        item.Path,
                        new BookId(item.BookId),
                        item.Title,
                        item.AuthorLine,
                        CreateLocation(historyLocation),
                        item.LastOpenedUtc);
                    if (history.Any(existing => PathsEqual(existing.BookPath, historyEntry.BookPath)))
                    {
                        throw new InvalidDataException("Il documento di stato contiene path duplicati nella cronologia.");
                    }
                    history.Add(historyEntry);
                }
            }
            else
            {
                string legacyTitle = Path.GetFileNameWithoutExtension(lastBook.Path);
                history.Add(new ReadingHistoryEntry(
                    lastBook.Path,
                    new BookId(lastBook.BookId),
                    string.IsNullOrWhiteSpace(legacyTitle) ? "Libro recente" : legacyTitle,
                    null,
                    readingLocation,
                    lastBook.LastOpenedUtc));
            }

            return new ReadingStateSnapshot(
                lastBook.Path,
                new BookId(lastBook.BookId),
                readingLocation,
                lastBook.LastOpenedUtc,
                bookmarks,
                history);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("Il documento di stato contiene valori non validi.", exception);
        }
    }

    private static ReadingLocation CreateLocation(LocationDto location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location.SectionId);
        SectionId sectionId = new(location.SectionId);
        BlockId? blockId = string.IsNullOrWhiteSpace(location.BlockId)
            ? null
            : new BlockId(location.BlockId);
        return new ReadingLocation(sectionId, blockId, location.CharacterOffset);
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
                Location = ToLocation(state.Location),
            },
            Bookmarks = state.Bookmarks
                .Select(bookmark => new BookmarkDto
                {
                    Path = bookmark.BookPath,
                    BookId = bookmark.BookId.Value,
                    Location = ToLocation(bookmark.Location),
                })
                .ToList(),
            History = state.History
                .Select(item => new HistoryDto
                {
                    Path = item.BookPath,
                    BookId = item.BookId.Value,
                    Title = item.Title,
                    AuthorLine = item.AuthorLine,
                    LastOpenedUtc = item.LastOpenedUtc,
                    Location = ToLocation(item.Location),
                })
                .ToList(),
        };

    private static LocationDto ToLocation(ReadingLocation location) =>
        new()
        {
            SectionId = location.SectionId.Value,
            BlockId = location.BlockId?.Value,
            CharacterOffset = location.CharacterOffset,
        };

    private static bool PathsEqual(string left, string right) =>
        string.Equals(left, right, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

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

        public List<BookmarkDto>? Bookmarks { get; init; }

        public List<HistoryDto>? History { get; init; }
    }

    private sealed class LastBookDto
    {
        public string? Path { get; init; }

        public string? BookId { get; init; }

        public DateTimeOffset LastOpenedUtc { get; init; }

        public LocationDto? Location { get; init; }
    }

    private sealed class BookmarkDto
    {
        public string? Path { get; init; }

        public string? BookId { get; init; }

        public LocationDto? Location { get; init; }
    }

    private sealed class HistoryDto
    {
        public string? Path { get; init; }
        public string? BookId { get; init; }
        public string? Title { get; init; }
        public string? AuthorLine { get; init; }
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
