using System.Text.Json;
using System.Text.Json.Serialization;

namespace EbookReader.Cli.Configuration;

/// <summary>
/// Small, versioned and atomically-written user preference file. This file is deliberately
/// separate from reading state because theme/key bindings are user configuration, not book state.
/// </summary>
public sealed class JsonReaderPreferencesStore
{
    public const int CurrentSchemaVersion = 1;
    public const long MaximumConfigBytes = 65_536;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public JsonReaderPreferencesStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public ReaderPreferences Load()
    {
        if (!File.Exists(FilePath))
        {
            return ReaderPreferences.Default;
        }

        FileInfo info = new(FilePath);
        if (info.Length > MaximumConfigBytes)
        {
            throw new InvalidDataException($"Il file di configurazione supera il limite di {MaximumConfigBytes} byte.");
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
            PreferencesDocumentDto? document = JsonSerializer.Deserialize<PreferencesDocumentDto>(stream, SerializerOptions);
            if (document is null)
            {
                throw new InvalidDataException("Il file di configurazione JSON è vuoto.");
            }

            return FromDocument(document);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Il file di configurazione JSON non è valido.", exception);
        }
    }

    public void Save(ReaderPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        string? directory = Path.GetDirectoryName(FilePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Il percorso del file di configurazione non ha una directory valida.");
        }

        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");
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
                JsonSerializer.Serialize(stream, ToDocument(preferences), SerializerOptions);
                stream.Flush(flushToDisk: true);
            }

            if (new FileInfo(temporaryPath).Length > MaximumConfigBytes)
            {
                throw new InvalidOperationException(
                    $"La configurazione serializzata supera il limite di {MaximumConfigBytes} byte.");
            }

            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        finally
        {
            DeleteTemporaryFileIfPresent(temporaryPath);
        }
    }

    private static ReaderPreferences FromDocument(PreferencesDocumentDto document)
    {
        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Versione schema configurazione non supportata: {document.SchemaVersion}.");
        }

        string themeId = string.IsNullOrWhiteSpace(document.Theme)
            ? ReaderThemeIds.SemanticDark
            : document.Theme;
        Dictionary<ReaderCommand, string> overrides = [];
        if (document.Keymap is not null)
        {
            foreach ((string commandName, string binding) in document.Keymap)
            {
                if (!Enum.TryParse(commandName, ignoreCase: true, out ReaderCommand command)
                    || !Enum.IsDefined(command))
                {
                    throw new InvalidDataException($"Comando keymap sconosciuto: {commandName}.");
                }

                if (!overrides.TryAdd(command, binding))
                {
                    throw new InvalidDataException($"Comando keymap duplicato: {commandName}.");
                }
            }
        }

        try
        {
            return new ReaderPreferences(themeId, ReaderKeymap.Create(overrides));
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("La configurazione contiene tema o keymap non validi.", exception);
        }
    }

    private static PreferencesDocumentDto ToDocument(ReaderPreferences preferences) =>
        new()
        {
            SchemaVersion = CurrentSchemaVersion,
            Theme = preferences.ThemeId,
            Keymap = preferences.Keymap.Bindings.ToDictionary(
                pair => pair.Key.ToString(),
                pair => pair.Value,
                StringComparer.Ordinal),
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
            // Same rationale as above; the destination configuration is never deleted here.
        }
    }

    private sealed class PreferencesDocumentDto
    {
        public int SchemaVersion { get; init; }

        public string? Theme { get; init; }

        public Dictionary<string, string>? Keymap { get; init; }
    }
}
