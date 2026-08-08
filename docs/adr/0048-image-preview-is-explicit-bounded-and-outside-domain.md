# ADR-0048 — Image preview is explicit, bounded and outside the Domain

- Status: Accepted
- Date: 2026-08-08

## Context

EReader already projects EPUB images into the format-neutral Domain as `ImageBlock` plus a payload-free `BookResource`. The deterministic layout renders a textual placeholder using alternative text/caption. M3.4 needs a useful way to inspect an illustration without turning the TUI into a browser or moving binary payloads into Domain/state.

Opening arbitrary EPUB resources automatically would create several problems: hidden I/O during navigation, accidental network access for remote resources, active SVG/browser content, unbounded memory/filesystem use and a coupling between layout coordinates and binary resources.

## Decision

Image preview is an explicit outer-adapter operation.

- The logical reader remains text-first: layout continues to render the existing image placeholder.
- `ReaderSession.CurrentImage` exposes only format-neutral metadata for the `ImageBlock` at the current `ReadingLocation`.
- `Enter` in the normal reader invokes preview only when the current logical block is an image.
- `EpubImageResourceReader` reopens the already selected EPUB and resolves the manifest item by `ResourceId`.
- Only local `image/jpeg`, `image/png`, `image/gif` and `image/webp` resources are previewable.
- SVG is deliberately not launched because it can contain active/external content and is commonly opened by browsers.
- Remote resources are never retrieved.
- Image payload is bounded to 16 MiB and exists in memory only for the explicit preview operation.
- The CLI writes one resource to a private temporary directory with an extension derived from the manifest media type, never from untrusted filename text, then asks the operating system to open it with its associated viewer.
- Temporary files are best-effort deleted when the reader host is disposed. Failure to clean up is non-fatal because an external viewer may still hold the file.
- No image bytes/path are persisted in `state.json` or `config.json`.

## Consequences

Positive:

- Domain remains format-neutral and payload-free.
- Layout/search/progress/bookmarks remain unchanged.
- No browser engine or image decoder dependency is added to EReader.
- No automatic network or archive extraction is introduced.
- The user controls when potentially complex image decoding is delegated to another application.

Trade-offs:

- Preview depends on an OS file association and may be unavailable on headless/minimal systems.
- The external viewer is a separate trust boundary and may have its own decoder vulnerabilities.
- A temporary file can remain when the external process keeps it locked or cleanup is denied.
- SVG remains textual-only in M3.4.
