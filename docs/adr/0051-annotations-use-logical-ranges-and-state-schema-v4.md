# ADR-0051 — Annotations use logical ranges and state schema v4

- Status: Accepted — M3.7 Hotfix 1 VALIDATED
- Date: 2026-08-08

## Context

EReader already persists resume locations, bookmarks and history without layout coordinates. Highlights and personal notes must survive terminal resize, wrapping changes and theme changes, and must not force annotation concepts into the format-neutral layout engine.

## Decision

1. `state.json` advances from schema 3 to schema 4. Schema 1, 2 and 3 remain readable.
2. A highlight is persisted as `BookPath + BookId + Start ReadingLocation + End ReadingLocation`. Start is inclusive and End is exclusive in UTF-16 code units.
3. M3.7 creates highlight ranges only inside one Domain block. This keeps validation deterministic while leaving room for a future multi-block selection model.
4. A personal note is persisted as `BookPath + BookId + ReadingLocation + text + updatedUtc`.
5. Highlight/note restoration requires matching path, `BookId` and valid logical locations; same-path annotations are replaced as a unit when saving so stale annotations disappear if the publication at that path changes identity.
6. Annotation data is bounded: 1,000 highlights total / 250 per book; 500 notes total / 100 per book; 2,048 UTF-16 code units per note; 131,072 total note-text code units; the existing 1 MiB state-file cap remains authoritative.
7. F2/F3/F4 are fixed Terminal.Gui special keys for highlight, note editing and annotation list. `config.json` remains schema 1, avoiding migration/collision problems with existing printable keymaps.
8. The layout engine remains annotation-unaware. `ReaderBodyView` receives logical highlight ranges and paints any visual line that intersects them using the active theme highlight attribute. The stored range remains exact even though M3.7 rendering is line-granular.
9. Notes are edited through a bounded inline TUI prompt. The annotations overlay lists highlights and notes together; Enter navigates and the existing contextual delete binding removes the selected item.

## Consequences

- Resize/reflow cannot invalidate persisted highlights or notes.
- `Domain`, EPUB parsing and `EbookReader.Layout` do not gain user-annotation or persistence dependencies.
- State schema evolves explicitly while configuration schema does not.
- M3.7 does not implement arbitrary mouse/character selection or cross-block highlight creation; those can be layered later without changing the durable coordinate system.
