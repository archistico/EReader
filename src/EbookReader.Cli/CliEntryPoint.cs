using System.Globalization;
using System.Collections.ObjectModel;
using System.Reflection;
using EbookReader.Application.Library;
using EbookReader.Application.State;
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
    public const string Milestone = "M3.0";
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

        WriteDiagnostics(result, error);

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
        ReaderRunResult runResult = TerminalGuiReaderHost.Run(book, initialLocation, initialBookmarks);

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
            ReadingStateSnapshot state = new(
                filePath,
                book.Id,
                runResult.Location,
                openedUtc,
                bookmarks,
                history);
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

    private static void WriteDiagnostics(EpubValidationResult result, TextWriter error)
    {
        foreach (EpubDiagnostic diagnostic in result.Diagnostics)
        {
            error.Write('[');
            error.Write(diagnostic.Severity.ToString().ToUpperInvariant());
            error.Write(' ');
            error.Write(diagnostic.Code);
            error.Write("] ");
            error.WriteLine(diagnostic.Message);
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
        output.WriteLine("Library/history: M3.0 recent-book JSON library with --library/--history candidate");
    }

    private static void WriteHelp(TextWriter output)
    {
        output.WriteLine("EReader — M3.0 Library & Reading History");
        output.WriteLine();
        output.WriteLine("Uso:");
        output.WriteLine("  ereader <libro.epub>          apre il reader fullscreen");
        output.WriteLine("  ereader --resume              riapre l'ultimo libro e la posizione salvata");
        output.WriteLine("  ereader --library             apre la libreria recente interattiva");
        output.WriteLine("  ereader --history             stampa la cronologia recente su stdout");
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
        output.WriteLine("  F1 / ?         aiuto");
        output.WriteLine("  q              esci e salva la ReadingLocation");
        output.WriteLine("  Esc            annulla ricerca o chiude bookmark/metadati/indice/aiuto, altrimenti esce");
    }
}
