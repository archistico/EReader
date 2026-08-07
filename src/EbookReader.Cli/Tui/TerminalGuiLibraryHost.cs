using System.Collections.ObjectModel;
using EbookReader.Application.Library;
using Terminal.Gui.App;

namespace EbookReader.Cli.Tui;

internal static class TerminalGuiLibraryHost
{
    public static LibraryRunResult Run(ReadOnlyCollection<ReadingHistoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        using IApplication app = global::Terminal.Gui.App.Application.Create().Init();
        using LibraryWindow window = new(entries);
        app.Run(window);
        return new LibraryRunResult(window.SelectedEntry);
    }
}
