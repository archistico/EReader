# ADR-0049 — Hyperlinks use logical ranges and a transient back stack

- Status: Accepted for M3.5 candidate
- Date: 2026-08-08

## Context

EPUB XHTML hyperlinks are already represented in the format-neutral Domain as `HyperlinkSpan` with either an `InternalLinkTarget` (`ReadingLocation`) or an `ExternalLinkTarget` (`Uri`). Until M3.5 the TUI renders their text but cannot activate them.

A terminal reader must not make hyperlink behavior depend on page number, wrapped line number or terminal dimensions. External links also require an explicit trust boundary: reading an EPUB must never launch a browser, access the network or execute a URI automatically.

## Decision

- `EbookReader.Application.Links.BookHyperlinkIndex` scans Domain inline content before layout and records hyperlink ranges in UTF-16 logical offsets, the same coordinate system used by `ReadingLocation`.
- Empty hyperlink text is not actionable because it has no visible/logical range.
- At the current reader location, an exact hyperlink containing `ReadingLocation.CharacterOffset` has priority. Otherwise the first hyperlink intersecting the current `VisualLine` source range is offered.
- `Enter` activates the offered hyperlink. If no hyperlink is offered, M3.4 image preview remains the Enter fallback.
- Internal links navigate directly to their already-resolved Domain `ReadingLocation`.
- Before an internal hyperlink jump, `ReaderSession` pushes the origin into a transient bounded back stack. `Backspace` pops that stack. Maximum depth is 128 origins; the oldest origin is discarded when full.
- The back stack is not persisted. `state.json` remains schema 3 and `config.json` remains schema 1.
- External links never alter `ReadingLocation` or the back stack. The CLI outer adapter alone delegates them to the OS shell after an explicit Enter.
- The OS adapter repeats a strict allow-list: `http`, `https`, `mailto`. It uses `UseShellExecute = true`; unsupported schemes are rejected.
- The EPUB parser's existing policy continues to strip action semantics from `file:`, `javascript:`, `data:` and other unsupported schemes.
- No network client, browser engine or URI fetcher is added to EReader.

## Consequences

Positive:

- Internal navigation remains stable across resize and wrapping.
- Footnotes/endnotes can build on the same internal-link + back-stack primitive in M3.6.
- External launching stays explicit and confined to the CLI boundary.
- No persisted schema migration is required.
- Existing image Enter behavior remains available when no hyperlink is actionable.

Trade-offs:

- When several links occur on one wrapped line and the current logical offset is not inside one, M3.5 selects the first intersecting link deterministically.
- The back stack records hyperlink jumps only; it is not a general browser-style history of every reader movement.
- OS handling of `http`, `https` and `mailto` depends on system associations and may fail on headless/minimal environments.
