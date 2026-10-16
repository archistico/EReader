# EReader

**EReader by Emilie Rollandin** is a terminal reader for **reflowable EPUB 2 and EPUB 3 books**. It provides a fullscreen reading interface, remembers your reading position between sessions, and opens books without converting or modifying the original EPUB file.

## Features

- Fullscreen reading in the terminal.
- Interactive table of contents and chapter navigation.
- Full-text search.
- Persistent bookmarks, highlights, and personal notes.
- Stable reading progress across layout changes.
- Reading history and a recent-books library.
- Configurable themes and keyboard shortcuts.
- Internal links, EPUB footnotes and endnotes, and navigation back to the previous position.
- Explicit opening of external `http`, `https`, and `mailto` links.
- Preview of local raster images.
- Clear diagnostics for damaged, incomplete, or unsupported EPUB files.
- Protection against path traversal, malformed ZIP archives, and other hostile EPUB structures.

EReader does not support DRM-protected books, PDF, MOBI, AZW3, or FB2.

## Installation

### From a GitHub Release

1. Download the archive for your system from the project's GitHub Releases:
   - `EReader-<version>-win-x64.zip` for 64-bit Windows.
   - `EReader-<version>-linux-x64.tar.gz` for 64-bit Linux.
2. Extract the archive into a folder of your choice.
3. Open a terminal in that folder and start EReader.

The release scripts produce **self-contained** packages, so users do not need to install .NET separately.

Windows (PowerShell):

```powershell
.\ereader.exe "C:\Books\book.epub"
```

Linux:

```sh
./ereader "/home/user/Books/book.epub"
```

If the executable permission is missing after extraction on Linux:

```sh
chmod +x ereader
```

### Build and run from source

Install the **[.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)**. The repository's `global.json` requires SDK version `10.0.100` or a later stable .NET 10 feature band. The SDK is needed to build the application; it is not needed to run a self-contained release.

Run these commands from the repository root:

```sh
dotnet restore EbookReader.sln
dotnet build EbookReader.sln -c Release --no-restore
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- "book.epub"
```

Replace `book.epub` with the path to your book. Fullscreen mode requires an interactive terminal; use `--plain` when redirecting output.

## Build a portable Windows executable

With the .NET 10 SDK installed, open PowerShell in the repository root and run:

```powershell
dotnet publish src/EbookReader.Cli/EbookReader.Cli.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false -p:DebugType=None -o artifacts/portable/win-x64
```

This command restores the required packages and creates:

```text
artifacts/portable/win-x64/ereader.exe
```

You can copy `ereader.exe` to another folder, a USB drive, or another compatible 64-bit Windows computer and run it without an installer or a separate .NET installation. EPUB files are supplied separately.

Check the executable and open a book:

```powershell
.\artifacts\portable\win-x64\ereader.exe --version
.\artifacts\portable\win-x64\ereader.exe "C:\Books\book.epub"
```

The publishing options have the following effects:

| Option | Purpose |
|---|---|
| `-c Release` | Builds the release configuration. |
| `-r win-x64` | Targets 64-bit Windows. |
| `--self-contained true` | Includes the .NET runtime. |
| `-p:PublishSingleFile=true` | Bundles the application into one executable. |
| `-p:IncludeNativeLibrariesForSelfExtract=true` | Bundles native libraries and extracts them when needed at startup. |
| `-p:DebugSymbols=false -p:DebugType=None` | Omits separate debug symbol files. |
| `-o artifacts/portable/win-x64` | Selects the output folder. |

The executable is specific to its target operating system and architecture. Bundled native libraries are extracted under `%TEMP%\.net` on Windows by default. See Microsoft's [single-file deployment documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview) for details.

### Keep reading data with the portable executable

By default, EReader stores reading data and preferences in the current user's local application data folder. To keep these files with the executable, open PowerShell in the folder containing `ereader.exe` and set the paths before launching it:

```powershell
$env:EREADER_STATE_FILE = Join-Path (Get-Location).Path "data\state.json"
$env:EREADER_CONFIG_FILE = Join-Path (Get-Location).Path "data\config.json"
.\ereader.exe --init-config
.\ereader.exe ".\Books\book.epub"
```

These variables apply to the current PowerShell session. Set them again in each new session, or put the commands in a launcher script. The folder must be writable. Copy the `data` folder along with the executable to carry your saved progress, bookmarks, annotations, history, and preferences. If book paths change after moving the folder or USB drive, open the book at its new path.

### Create release archives

To build versioned packages for both Windows x64 and Linux x64, use the existing release scripts from the repository root.

Windows (PowerShell; requires `dotnet` and `tar`):

```powershell
.\publish-release.cmd 1.0.0
```

Linux (requires `dotnet`, `tar`, and `zip`):

```sh
./publish-release.sh 1.0.0
```

Replace `1.0.0` with the intended release version. The scripts clean the projects' `bin` and `obj` folders, run the full validation gate, and package the self-contained executables with this README. They recreate the output directory for the selected version and produce:

```text
artifacts/release/v1.0.0/EReader-1.0.0-win-x64.zip
artifacts/release/v1.0.0/EReader-1.0.0-linux-x64.tar.gz
```

The scripts create local archives; uploading them to GitHub is a separate step. See [`docs/RELEASE.md`](docs/RELEASE.md) for the release workflow.

## Usage

The examples below assume `ereader` is on your `PATH`. When running from its folder, use `.\ereader.exe` in PowerShell or `./ereader` on Linux.

```text
ereader book.epub            Open the fullscreen reader
ereader --resume             Reopen the last book at the saved position
ereader --library            Open the recent-books library
ereader --history            Print recent reading history
ereader --plain book.epub    Print readable book content to standard output
ereader --help               Show all commands
ereader --version            Show the application version
```

For technical details when troubleshooting an internal error:

```text
ereader --diagnostics book.epub
```

EReader distinguishes internal reader errors from unreadable EPUB files, so a reader failure is not automatically reported as a problem with the book.

## Reading controls

| Key | Action |
|---|---|
| `Up` / `Down` | Previous / next line |
| `PgUp` / `PgDn` | Previous / next page |
| `Space` | Next page |
| `[` / `]` | Previous / next chapter |
| `g` / `G` | Start / end of chapter |
| `t` or `Tab` | Table of contents |
| `/` | Search |
| `n` / `N` | Next / previous search result |
| `b` | Add or remove a bookmark |
| `B` | Bookmark list |
| `m` | Book metadata |
| `c` | Change theme |
| `Enter` | Follow a link or note; otherwise open the current image |
| `Backspace` | Go back after following an internal link |
| `F2` | Add or remove a highlight |
| `F3` | Add or edit a personal note |
| `F4` | Annotation list |
| `F1` | Help |
| `q` | Quit and save the reading position |

Printable-key shortcuts can be customized. Arrow keys, `PgUp` / `PgDn`, `Space`, `Tab`, `Enter`, `Backspace`, `Esc`, and `F1`–`F4` remain available.

The reader's command bar uses two lines and automatically switches to a compact layout in narrower terminals.

## Data and configuration

EReader saves reading positions, bookmarks, notes, highlights, history, and preferences locally. It does not modify the EPUB file.

By default, the files are `EReader/state.json` and `EReader/config.json` under the operating system's local application data folder. On Windows, this is normally `%LOCALAPPDATA%\EReader\`.

To display the configuration path or create the default configuration:

```text
ereader --config-path
ereader --init-config
```

Override the paths with `EREADER_STATE_FILE` and `EREADER_CONFIG_FILE` for portable setups or tests, as shown above.

## Damaged or unsupported EPUB files

EReader tries to continue when a problem is nonessential, such as a missing optional resource. When the structure required for reading is unusable, it rejects that book and reports an explicit diagnostic.

The project's guiding principle is:

> **An EPUB may be unreadable. EReader must remain usable.**

EReader treats every EPUB as untrusted input. It does not extract the book archive onto the filesystem or execute code contained in the book.

## Development checks

Run the full validation gate from the repository root.

Windows (PowerShell):

```powershell
.\validate.cmd
```

Linux:

```sh
./validate.sh
```

The gate restores packages, builds in Release mode, runs the tests, and checks CLI commands and the EPUB reliability corpus. See [`docs/VALIDATION.md`](docs/VALIDATION.md) for details.

## Documentation

The full documentation is indexed in [`docs/README.md`](docs/README.md). The technical documents are currently written in Italian.

- [`docs/EPUB_COMPATIBILITY.md`](docs/EPUB_COMPATIBILITY.md): supported EPUB features and limitations.
- [`docs/CONFIGURATION_KEYMAP.md`](docs/CONFIGURATION_KEYMAP.md): preferences and keyboard configuration.
- [`docs/VALIDATION.md`](docs/VALIDATION.md): the repository validation gate.
- [`docs/RELEASE.md`](docs/RELEASE.md): release packaging.
- [`docs/ROADMAP.md`](docs/ROADMAP.md): planned development.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md): components and dependencies.
