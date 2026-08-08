# M3.6 — Footnotes / Endnotes UX

M3.6 builds directly on the validated M3.5 hyperlink primitive. It does not introduce a separate note document model or a browser-like popup system.

## Recognition

At the EPUB boundary, an XHTML anchor whose `epub:type` token list contains `noteref` is mapped to the format-neutral Domain role:

```text
epub:type="noteref"
        ↓ EPUB adapter only
HyperlinkRole.NoteReference
        ↓
BookHyperlinkIndex
        ↓
ReadingLocation target
```

The target can be a footnote or endnote anywhere in the publication reading order. The target element's EPUB vocabulary does not need to leak into Domain because navigation is already represented by its resolved `ReadingLocation`.

## Reader UX

When the current logical position is inside, or the current visual line intersects, a note-reference link:

- header shows `NOTA`;
- footer shows `Enter nota`;
- Enter follows the internal target;
- status reports `Nota aperta. Backspace torna al testo.`;
- Backspace pops the same bounded transient stack introduced by M3.5.

A note remains ordinary book content after the jump. Line/page navigation, search, resize/reflow, themes, bookmarks and stable progress therefore continue to operate normally.

## Compatibility

A publication that does not mark note links with `epub:type="noteref"` is unchanged: the link remains a generic internal hyperlink and is still actionable through M3.5. Multiple tokens are accepted; `noteref` is recognized case-insensitively within the whitespace-separated token list.

## Persistence

M3.6 changes neither persisted schema:

- `state.json`: schema 3;
- `config.json`: schema 1.

No note popup state, EPUB semantic token, page number, line number or back-stack entry is persisted.
