# M3.5 — Interactive Hyperlinks & Back Stack

M3.5 activates the hyperlink semantics already present in the Domain without turning EReader into a browser.

## User interaction

In normal reading mode:

- `Enter` follows the hyperlink associated with the current logical position.
- If the exact `ReadingLocation` is not inside a link, EReader offers the first link intersecting the current visual line.
- If no link is actionable, `Enter` keeps the M3.4 behavior and previews the current raster image when available.
- `Backspace` returns to the origin of the latest internal hyperlink jump.

The header shows `LINK interno`, `LINK http`, `LINK https` or `LINK mailto` when a link is actionable. The footer exposes `Enter link` and, when the stack is non-empty, `Backspace indietro`.

## Logical model

`BookHyperlinkIndex` lives in `EbookReader.Application.Links` and indexes the Domain before layout:

```text
HyperlinkSpan
    ↓
logical text range in UTF-16
    ↓
ReadingLocation start + length
    ↓
BookHyperlink
```

The range uses exactly the same UTF-16 coordinate system as search, anchors, progress and `ReadingLocation.CharacterOffset`.

An exact logical match takes priority. Otherwise ReaderSession uses the `VisualLine.SourceStartOffset` / `SourceEndOffset` mapping only to identify which logical range is visible; the hyperlink itself remains independent of wrapping.

## Internal links

Internal EPUB links have already been resolved by `EpubBookReader` to a valid `InternalLinkTarget.Location`.

On Enter:

1. save the current `ReadingLocation` in the transient link stack;
2. jump to the target `ReadingLocation`;
3. preserve all normal resize/reflow guarantees;
4. expose Backspace as a return action.

The stack is bounded to 128 origins and is never serialized.

## External links

External links are delegated only after explicit user action to `SystemExternalLinkService` in the CLI layer.

Allowed schemes:

- `http`
- `https`
- `mailto`

Rejected/non-actionable schemes include `file`, `javascript`, `data` and any scheme not allowed by the EPUB ingestion policy. EReader itself does not fetch the URI and does not add a browser/network dependency.

## Persistence

M3.5 changes neither persisted schema:

- `state.json`: schema 3;
- `config.json`: schema 1.

The back stack and currently offered hyperlink are runtime-only state.

## Deliberate limitation

M3.5 does not yet introduce dedicated next-link/previous-link keys. If several links are on the same visual line, the first intersecting link is selected unless the current logical offset lies inside a specific link. This deterministic rule is sufficient for the first interactive-link milestone and keeps keymap compatibility intact.


## M3.6 note-reference specialization

M3.6 keeps the M3.5 hyperlink index/back-stack intact and adds only a format-neutral `HyperlinkRole.NoteReference`. The EPUB adapter maps `epub:type="noteref"` to that role; the TUI uses it to present `NOTA` / `Enter nota` and a note-specific return message. See `FOOTNOTES_ENDNOTES.md`.
