# ADR-0050 — Note references are format-neutral hyperlink roles

- Status: Accepted for M3.6 candidate
- Date: 2026-08-08

## Context

M3.5 made Domain hyperlinks actionable through logical UTF-16 ranges and a transient back stack. EPUB 3 publications commonly mark a link to a footnote or endnote with `epub:type="noteref"`. Treating that token only as an EPUB parsing detail would lose useful reading semantics before the Application/TUI layers can present note-specific UX.

## Decision

- The format-neutral Domain adds `HyperlinkRole` with `Generic` and `NoteReference`.
- `HyperlinkSpan` stores the role and defaults to `Generic`, preserving existing callers.
- Only the EPUB adapter knows the source token `epub:type="noteref"`; it maps that token to `HyperlinkRole.NoteReference`.
- `BookHyperlinkIndex` preserves the role while keeping the same UTF-16 logical ranges as M3.5.
- A note reference remains an ordinary internal `ReadingLocation` target. Enter follows it through the existing M3.5 back stack; Backspace returns to the origin.
- The TUI may label the action as `NOTA` / `Enter nota`, but no EPUB vocabulary leaks into Application, Layout or persistence.
- M3.6 does not persist note mode, back-stack entries or layout coordinates.

## Consequences

The reader gains explicit footnote/endnote affordances while retaining one navigation primitive. Publications lacking `noteref` still work as generic internal hyperlinks. Future richer note presentation can build on the same Domain role without changing `ReadingLocation`.
