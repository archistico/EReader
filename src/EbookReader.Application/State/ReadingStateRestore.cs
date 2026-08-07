using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.State;

/// <summary>
/// Applies a durable snapshot only when it still identifies the same publication and a valid logical location.
/// </summary>
public static class ReadingStateRestore
{
    public static ReadingLocation? TryGetLocation(
        Book book,
        string currentBookPath,
        ReadingStateSnapshot? state)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentBookPath);

        if (state is null)
        {
            return null;
        }

        string currentFullPath = Path.GetFullPath(currentBookPath);
        StringComparison pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!string.Equals(currentFullPath, state.BookPath, pathComparison)
            || state.BookId != book.Id
            || !book.ContainsLocation(state.Location))
        {
            return null;
        }

        return state.Location;
    }
}
