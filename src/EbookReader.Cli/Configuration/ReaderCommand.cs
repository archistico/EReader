namespace EbookReader.Cli.Configuration;

/// <summary>
/// Printable reader commands whose keyboard aliases can be customized independently from
/// terminal special keys such as arrows, PageUp/PageDown, Enter, Escape and F1.
/// </summary>
public enum ReaderCommand
{
    PreviousLine = 0,
    NextLine = 1,
    PreviousPage = 2,
    NextPage = 3,
    PreviousChapter = 4,
    NextChapter = 5,
    ChapterStart = 6,
    ChapterEnd = 7,
    ToggleToc = 8,
    Search = 9,
    NextSearchResult = 10,
    PreviousSearchResult = 11,
    ToggleBookmark = 12,
    OpenBookmarks = 13,
    ToggleMetadata = 14,
    CycleTheme = 15,
    Help = 16,
    Quit = 17,
    DeleteBookmark = 18,
}
