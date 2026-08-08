using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using EbookReader.Application.Annotations;
using EbookReader.Application.Diagnostics;
using EbookReader.Application.Library;
using EbookReader.Application.State;
using EbookReader.Cli.Configuration;
using EbookReader.Cli.Diagnostics;
using EbookReader.Cli.Reading;
using EbookReader.Cli.State;
using EbookReader.Cli.Tui;
using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;
using EbookReader.Epub.Validation;

namespace EbookReader.Cli;

/// <summary>
/// EReader command-line composition root.
/// </summary>
public static class CliEntryPoint
{
    public const string Milestone = "M3.8";
    public const string Status = "CANDIDATE";

    private const int Success = 0;
    private const int UsageError = 2;
    private const int InvalidPublication = 3;
    private const int UnsupportedPublication = 4;
    private const int IoFailure = 5;

    public static int Run(string[] args) => Run(args, Console.Out, Console.Error);

    /// <summary>
    /// Testable/hostable entry point. Plain reading output is written only to <paramref name="output"/>,
    /// while diagnostics and usage errors are written to <paramref name="error"/>.
    /// </summary>
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args.Length == 0)
        {
            WriteHelp(output);
            return Success;
        }

        if (args.Length == 1 && string.Equals(args[0], "--help", StringComparison.Ordinal))
        {
            WriteHelp(output);
            return Success;
        }

        if (args.Length == 1 && string.Equals(args[0], "--version", StringComparison.Ordinal))
        {
            output.WriteLine(GetProductVersion());
            return Success;
        }

        if (args.Length == 1 && string.Equals(args[0], "--foundation-info", StringComparison.Ordinal))
        {
            WriteFoundationInfo(output);
            return Success;
        }

        if (args.Length == 1 && string.Equals(args[0], "--config-path", StringComparison.Ordinal))
        {
            return WriteConfigPath(output, error);
        }

        if (args.Length == 1 && string.Equals(args[0], "--init-config", StringComparison.Ordinal))
        {
            return InitializeConfig(output, error);
        }

        if (args.Length == 1 && string.Equals(args[0], "--library", StringComparison.Ordinal))
        {
            return BrowseLibrary(output, error);
        }

        if (args.Length == 1 && string.Equals(args[0], "--history", StringComparison.Ordinal))
        {
            return WriteHistory(output, error);
        }

        if (args.Length == 1 && string.Equals(args[0], "--resume", StringComparison.Ordinal))
        {
            return ResumePublication(output, error);
        }

        if (args.Length == 2 && string.Equals(args[0], "--plain", StringComparison.Ordinal))
        {
            return ReadPublication(args[1], output, error, interactive: false);
        }

        if (args.Length == 1 && !args[0].StartsWith('-'))
        {
            return ReadPublication(args[0], output, error, interactive: true);
        }

        error.WriteLine("Argomenti non validi. Usa --help.");
        return UsageError;
    }

    public static string GetTerminalGuiVersion()
    {
        Assembly assembly = typeof(global::Terminal.Gui.App.Application).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }

    private static int BrowseLibrary(TextWriter output, TextWriter error)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            error.WriteLine("ER-LIBRARY-TUI-001: la libreria interattiva richiede un terminale. Usa 'ereader --history'.");
            return UsageError;
        }

        JsonReadingStateStore? store = TryCreateStateStore(error);
        if (store is null)
        {
            return UsageError;
        }

        ReadingStateSnapshot? state = TryLoadState(store, error);
        if (state is null || state.History.Count == 0)
        {
            error.WriteLine("ER-LIBRARY-EMPTY-001: nessun libro recente disponibile.");
            return UsageError;
        }

        LibraryRunResult selection = TerminalGuiLibraryHost.Run(state.History);
        if (selection.SelectedEntry is null)
        {
            return Success;
        }

        return ReadPublication(
            selection.SelectedEntry.BookPath,
            output,
            error,
            interactive: true,
            stateStore: store,
            preloadedState: state,
            historyEntry: selection.SelectedEntry);
    }

    private static int WriteHistory(TextWriter output, TextWriter error)
    {
        JsonReadingStateStore? store = TryCreateStateStore(error);
        if (store is null)
        {
            return UsageError;
        }

        ReadingStateSnapshot? state = TryLoadState(store, error);
        if (state is null || state.History.Count == 0)
        {
            output.WriteLine("Nessun libro recente.");
            return Success;
        }

        foreach (ReadingHistoryEntry entry in state.History)
        {
            string author = string.IsNullOrWhiteSpace(entry.AuthorLine) ? string.Empty : $" — {entry.AuthorLine}";
            string timestamp = entry.LastOpenedUtc.ToString("u", CultureInfo.InvariantCulture);
            string missing = File.Exists(entry.BookPath) ? string.Empty : " [mancante]";
            output.WriteLine($"{timestamp}  {entry.Title}{author}{missing}");
            output.WriteLine($"  {entry.BookPath}");
        }

        return Success;
    }

    private static int ResumePublication(TextWriter output, TextWriter error)
    {
        JsonReadingStateStore? store = TryCreateStateStore(error);
        if (store is null)
        {
            return UsageError;
        }

        ReadingStateSnapshot? state = TryLoadState(store, error);
        if (state is null)
        {
            error.WriteLine("ER-STATE-RESUME-001: nessun ultimo libro salvato disponibile.");
            return UsageError;
        }

        return ReadPublication(
            state.BookPath,
            output,
            error,
            interactive: true,
            stateStore: store,
            preloadedState: state);
    }

    private static int ReadPublication(
        string filePath,
        TextWriter output,
        TextWriter error,
        bool interactive,
        JsonReadingStateStore? stateStore = null,
        ReadingStateSnapshot? preloadedState = null,
        ReadingHistoryEntry? historyEntry = null)
    {
        if (!File.Exists(filePath))
        {
            error.WriteLine($"ER-CLI-FILE-001: file EPUB non trovato: {filePath}");
            return UsageError;
        }

        EpubValidationResult result;
        try
        {
            result = EpubPublicationValidator.Validate(filePath);
        }
        catch (UnauthorizedAccessException exception)
        {
            error.WriteLine($"ER-CLI-IO-001: impossibile accedere al file EPUB: {exception.Message}");
            return IoFailure;
        }
        catch (IOException exception)
        {
            error.WriteLine($"ER-CLI-IO-002: errore di I/O durante la lettura dell'EPUB: {exception.Message}");
            return IoFailure;
        }

        ReaderOperationSummary operation = EpubReaderDiagnosticBridge.Create(result);
        ReaderDiagnosticTextWriter.Write(operation, error);

        return result.Status switch
        {
            EpubValidationStatus.Valid => ReadValidBook(
                result,
                filePath,
                output,
                error,
                interactive,
                stateStore,
                preloadedState,
                historyEntry),
            EpubValidationStatus.Invalid => InvalidPublication,
            EpubValidationStatus.Unsupported => UnsupportedPublication,
            _ => throw new InvalidOperationException($"Stato EPUB inatteso: {result.Status}."),
        };
    }

    private static int ReadValidBook(
        EpubValidationResult result,
        string filePath,
        TextWriter output,
        TextWriter error,
        bool interactive,
        JsonReadingStateStore? stateStore,
        ReadingStateSnapshot? preloadedState,
        ReadingHistoryEntry? historyEntry)
    {
        Book book = result.Book
            ?? throw new InvalidOperationException("Un risultato EPUB valido deve contenere un Book Domain.");

        if (!interactive)
        {
            BookConsoleRenderer.Write(book, output);
            return Success;
        }

        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            error.WriteLine("ER-CLI-TUI-001: la TUI richiede un terminale interattivo. Usa 'ereader --plain <libro.epub>'.");
            return UsageError;
        }

        JsonReadingStateStore? store = stateStore ?? TryCreateStateStore(error);
        ReadingStateSnapshot? savedState = preloadedState;
        if (savedState is null && store is not null)
        {
            savedState = TryLoadState(store, error);
        }

        ReadingHistoryEntry? matchingHistory = historyEntry
            ?? ReadingHistoryState.FindForBook(book, filePath, savedState?.History);
        ReadingLocation? initialLocation = matchingHistory is null
            ? ReadingStateRestore.TryGetLocation(book, filePath, savedState)
            : ReadingHistoryState.TryGetLocation(book, filePath, matchingHistory);
        ReadOnlyCollection<ReadingLocation> initialBookmarks = ReadingBookmarkState.RestoreForBook(book, filePath, savedState);
        ReadOnlyCollection<ReadingHighlightRange> initialHighlights = ReadingAnnotationState.RestoreHighlightsForBook(
            book,
            filePath,
            savedState?.Highlights);
        ReadOnlyCollection<ReadingPersonalNote> initialNotes = ReadingAnnotationState.RestoreNotesForBook(
            book,
            filePath,
            savedState?.Notes);
        JsonReaderPreferencesStore? preferencesStore = TryCreatePreferencesStore(error);
        ReaderPreferences preferences = ReaderPreferences.Default;
        if (preferencesStore is not null)
        {
            ReaderPreferences? loadedPreferences = TryLoadPreferences(preferencesStore, error);
            if (loadedPreferences is null)
            {
                preferencesStore = null;
            }
            else
            {
                preferences = loadedPreferences;
            }
        }

        ReaderRunResult runResult = TerminalGuiReaderHost.Run(
            book,
            filePath,
            initialLocation,
            initialBookmarks,
            initialHighlights,
            initialNotes,
            preferences);

        if (preferencesStore is not null
            && !string.Equals(runResult.ThemeId, preferences.ThemeId, StringComparison.Ordinal))
        {
            TrySavePreferences(preferencesStore, preferences.WithTheme(runResult.ThemeId), error);
        }

        if (store is not null)
        {
            ReadOnlyCollection<ReadingBookmarkSnapshot> bookmarks = ReadingBookmarkState.ReplaceForBook(
                book,
                filePath,
                savedState?.Bookmarks,
                runResult.Bookmarks);
            DateTimeOffset openedUtc = DateTimeOffset.UtcNow;
            ReadOnlyCollection<ReadingHistoryEntry> history = ReadingHistoryState.Update(
                book,
                filePath,
                savedState?.History,
                runResult.Location,
                openedUtc);
            ReadOnlyCollection<ReadingHighlightSnapshot> highlights = ReadingAnnotationState.ReplaceHighlightsForBook(
                book,
                filePath,
                savedState?.Highlights,
                runResult.Highlights);
            ReadOnlyCollection<ReadingPersonalNoteSnapshot> notes = ReadingAnnotationState.ReplaceNotesForBook(
                book,
                filePath,
                savedState?.Notes,
                runResult.Notes);
            ReadingStateSnapshot state = new(
                filePath,
                book.Id,
                runResult.Location,
                openedUtc,
                bookmarks,
                history,
                highlights,
                notes);
            TrySaveState(store, state, error);
        }

        return Success;
    }

    private static JsonReadingStateStore? TryCreateStateStore(TextWriter error)
    {
        try
        {
            return new JsonReadingStateStore(ReadingStatePathResolver.Resolve());
        }
        catch (ArgumentException exception)
        {
            error.WriteLine($"ER-STATE-PATH-001: persistenza disabilitata: {exception.Message}");
            return null;
        }
        catch (InvalidOperationException exception)
        {
            error.WriteLine($"ER-STATE-PATH-001: persistenza disabilitata: {exception.Message}");
            return null;
        }
    }

    private static ReadingStateSnapshot? TryLoadState(JsonReadingStateStore store, TextWriter error)
    {
        try
        {
            return store.Load();
        }
        catch (InvalidDataException exception)
        {
            error.WriteLine($"ER-STATE-LOAD-001: stato di lettura ignorato: {exception.Message}");
            return null;
        }
        catch (UnauthorizedAccessException exception)
        {
            error.WriteLine($"ER-STATE-LOAD-002: impossibile leggere lo stato: {exception.Message}");
            return null;
        }
        catch (IOException exception)
        {
            error.WriteLine($"ER-STATE-LOAD-002: impossibile leggere lo stato: {exception.Message}");
            return null;
        }
    }

    private static void TrySaveState(JsonReadingStateStore store, ReadingStateSnapshot state, TextWriter error)
    {
        try
        {
            store.Save(state);
        }
        catch (UnauthorizedAccessException exception)
        {
            error.WriteLine($"ER-STATE-SAVE-001: impossibile salvare lo stato: {exception.Message}");
        }
        catch (IOException exception)
        {
            error.WriteLine($"ER-STATE-SAVE-001: impossibile salvare lo stato: {exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            error.WriteLine($"ER-STATE-SAVE-001: impossibile salvare lo stato: {exception.Message}");
        }
    }

    private static int WriteConfigPath(TextWriter output, TextWriter error)
    {
        try
        {
            output.WriteLine(ReaderPreferencesPathResolver.Resolve());
            return Success;
        }
        catch (ArgumentException exception)
        {
            error.WriteLine($"ER-CONFIG-PATH-001: percorso configurazione non valido: {exception.Message}");
            return UsageError;
        }
        catch (InvalidOperationException exception)
        {
            error.WriteLine($"ER-CONFIG-PATH-001: percorso configurazione non disponibile: {exception.Message}");
            return UsageError;
        }
    }

    private static int InitializeConfig(TextWriter output, TextWriter error)
    {
        JsonReaderPreferencesStore? store = TryCreatePreferencesStore(error);
        if (store is null)
        {
            return UsageError;
        }

        if (File.Exists(store.FilePath))
        {
            output.WriteLine($"Configurazione già esistente: {store.FilePath}");
            return Success;
        }

        try
        {
            store.Save(ReaderPreferences.Default);
            output.WriteLine($"Configurazione predefinita creata: {store.FilePath}");
            return Success;
        }
        catch (UnauthorizedAccessException exception)
        {
            error.WriteLine($"ER-CONFIG-SAVE-001: impossibile creare la configurazione: {exception.Message}");
            return IoFailure;
        }
        catch (IOException exception)
        {
            error.WriteLine($"ER-CONFIG-SAVE-001: impossibile creare la configurazione: {exception.Message}");
            return IoFailure;
        }
        catch (InvalidOperationException exception)
        {
            error.WriteLine($"ER-CONFIG-SAVE-001: impossibile creare la configurazione: {exception.Message}");
            return IoFailure;
        }
    }

    private static JsonReaderPreferencesStore? TryCreatePreferencesStore(TextWriter error)
    {
        try
        {
            return new JsonReaderPreferencesStore(ReaderPreferencesPathResolver.Resolve());
        }
        catch (ArgumentException exception)
        {
            error.WriteLine($"ER-CONFIG-PATH-001: preferenze disabilitate: {exception.Message}");
            return null;
        }
        catch (InvalidOperationException exception)
        {
            error.WriteLine($"ER-CONFIG-PATH-001: preferenze disabilitate: {exception.Message}");
            return null;
        }
    }

    private static ReaderPreferences? TryLoadPreferences(JsonReaderPreferencesStore store, TextWriter error)
    {
        try
        {
            return store.Load();
        }
        catch (InvalidDataException exception)
        {
            error.WriteLine($"ER-CONFIG-LOAD-001: configurazione ignorata, uso default: {exception.Message}");
            return null;
        }
        catch (UnauthorizedAccessException exception)
        {
            error.WriteLine($"ER-CONFIG-LOAD-002: impossibile leggere la configurazione, uso default: {exception.Message}");
            return null;
        }
        catch (IOException exception)
        {
            error.WriteLine($"ER-CONFIG-LOAD-002: impossibile leggere la configurazione, uso default: {exception.Message}");
            return null;
        }
    }

    private static void TrySavePreferences(
        JsonReaderPreferencesStore store,
        ReaderPreferences preferences,
        TextWriter error)
    {
        try
        {
            store.Save(preferences);
        }
        catch (UnauthorizedAccessException exception)
        {
            error.WriteLine($"ER-CONFIG-SAVE-001: impossibile salvare le preferenze: {exception.Message}");
        }
        catch (IOException exception)
        {
            error.WriteLine($"ER-CONFIG-SAVE-001: impossibile salvare le preferenze: {exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            error.WriteLine($"ER-CONFIG-SAVE-001: impossibile salvare le preferenze: {exception.Message}");
        }
    }

    private static string GetProductVersion()
    {
        Assembly assembly = typeof(CliEntryPoint).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }

    private static void WriteFoundationInfo(TextWriter output)
    {
        output.WriteLine($"EReader {GetProductVersion()}");
        output.WriteLine($"Milestone: {Milestone} ({Status})");
        output.WriteLine("Target framework: net10.0");
        output.WriteLine($"Terminal.Gui assembly: {GetTerminalGuiVersion()}");
        output.WriteLine("Domain model: M0.2 format-neutral model available");
        output.WriteLine("EPUB container: M0.3 OCF bootstrap available");
        output.WriteLine("OPF package parsing: M0.4 available");
        output.WriteLine("EPUB navigation: M0.5 NCX/nav.xhtml available");
        output.WriteLine("Semantic content: M0.6 AngleSharp XHTML to Domain available");
        output.WriteLine("Validation diagnostics: M0.7 stable ingestion result available");
        output.WriteLine("Diagnostics foundation: M3.8 application-wide severity + operation outcome candidate");
        output.WriteLine("Readable EPUB CLI: M1.0 non-paginated Domain projection available via --plain");
        output.WriteLine("Deterministic layout: M1.1 viewport, Unicode wrapping and visual pages validated");
        output.WriteLine("Logical navigation: M1.2 ReadingLocation-based line/page/chapter navigation validated");
        output.WriteLine("Reader TUI: M1.3 Terminal.Gui 2.x fullscreen reader validated");
        output.WriteLine("Resize stability: M1.4 body-viewport reflow preserving ReadingLocation validated");
        output.WriteLine("Reading state: M2.0 versioned atomic JSON with logical-location restore validated");
        output.WriteLine("Line scrolling/UI separators: M2.0 Hotfix 1+2 validated");
        output.WriteLine("Interactive TOC: M2.1 hierarchical Domain TOC validated");
        output.WriteLine("Metadata view: M2.2 format-neutral metadata overlay validated");
        output.WriteLine("Pre-layout search: M2.3 logical-text search validated");
        output.WriteLine("Logical bookmarks: M2.4 schema 2 + semantic TUI colors validated");
        output.WriteLine("Stable progress: M2.5 logical UTF-16 progress independent of layout validated");
        output.WriteLine("Library/history: M3.0 recent-book JSON library with --library/--history validated");
        output.WriteLine("Library search: M3.1 transient ranked title/author/file/path filter validated");
        output.WriteLine("Reader themes: M3.2 three semantic palettes validated");
        output.WriteLine("Preferences/keymap: M3.3 separate config.json with printable aliases validated");
        output.WriteLine("Images: M3.4 bounded local raster preview through the system viewer validated");
        output.WriteLine("Hyperlinks: M3.5 logical internal navigation + transient back stack + explicit external OS handoff validated");
        output.WriteLine("Footnotes/endnotes: M3.6 EPUB noteref mapped to format-neutral note-reference UX validated");
        output.WriteLine("Annotations: M3.7 logical highlight ranges + personal notes in state schema 4 validated");
    }

    private static void WriteHelp(TextWriter output)
    {
        output.WriteLine("EReader — M3.8 Diagnostics Foundation & Failure Taxonomy");
        output.WriteLine();
        output.WriteLine("Uso:");
        output.WriteLine("  ereader <libro.epub>          apre il reader fullscreen");
        output.WriteLine("  ereader --resume              riapre l'ultimo libro e la posizione salvata");
        output.WriteLine("  ereader --library             apre la libreria recente interattiva");
        output.WriteLine("                               nella libreria: / cerca, Esc cancella filtro");
        output.WriteLine("  ereader --history             stampa la cronologia recente su stdout");
        output.WriteLine("  ereader --config-path         stampa il percorso di config.json");
        output.WriteLine("  ereader --init-config         crea config.json predefinito se assente");
        output.WriteLine("  ereader --plain <libro.epub> stampa il reading order su stdout");
        output.WriteLine("  ereader --help");
        output.WriteLine("  ereader --version");
        output.WriteLine("  ereader --foundation-info");
        output.WriteLine();
        output.WriteLine("TUI:");
        output.WriteLine("  ↑/k ↓/j       riga precedente/successiva");
        output.WriteLine("  PgUp/h PgDn/l  pagina precedente/successiva");
        output.WriteLine("  Space          pagina successiva");
        output.WriteLine("  [ ]            capitolo precedente/successivo");
        output.WriteLine("  g / G          inizio/fine capitolo");
        output.WriteLine("  t / Tab        apre/chiude indice");
        output.WriteLine("  /              cerca nel testo logico");
        output.WriteLine("  n / N          risultato successivo/precedente");
        output.WriteLine("  b              aggiunge/rimuove bookmark corrente");
        output.WriteLine("  B              apre/chiude elenco bookmark");
        output.WriteLine("  m              apre/chiude metadati");
        output.WriteLine("  c              cambia tema (persistito in config.json)");
        output.WriteLine("  Enter          segue link/rimando nota corrente; altrimenti apre l'immagine corrente");
        output.WriteLine("  Backspace      torna alla posizione precedente dopo un link interno");
        output.WriteLine("  F2             aggiunge/rimuove evidenziazione della riga corrente");
        output.WriteLine("  F3             aggiunge/modifica nota personale alla posizione corrente");
        output.WriteLine("  F4             apre/chiude elenco annotazioni");
        output.WriteLine("  F1 / ?         aiuto");
        output.WriteLine("  q              esci e salva la ReadingLocation");
        output.WriteLine("  Esc            annulla input o chiude annotazioni/bookmark/metadati/indice/aiuto, altrimenti esce");
        output.WriteLine();
        output.WriteLine("I tasti stampabili sono configurabili; frecce/PgUp/PgDn/Space/Tab/Enter/Backspace/Esc/F1-F4 restano fissi.");
        output.WriteLine("Annotazioni: evidenziazioni come range UTF-16 logici; note personali ancorate a ReadingLocation; state.json schema 4.");
        output.WriteLine("Note EPUB: epub:type=\"noteref\" usa ReadingLocation + Backspace per il ritorno immediato al testo.");
        output.WriteLine("Link: interni via ReadingLocation; esterni http/https/mailto soltanto su azione esplicita Enter.");
        output.WriteLine("Anteprima immagini: JPEG/PNG/GIF/WebP locali, max 16 MiB; SVG e risorse remote restano placeholder.");
        output.WriteLine("Override percorso configurazione: EREADER_CONFIG_FILE.");
    }
}
