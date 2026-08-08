# M3.7 — Highlights & Personal Notes

M3.7 adds persistent reader annotations without making page, line or viewport coordinates durable.

## Commands

| Key | Action |
|---|---|
| `F2` | Add/remove the highlight intersecting the current logical visual line |
| `F3` | Add/edit the personal note at the exact current `ReadingLocation` |
| `F4` | Open/close the combined annotations list |
| `↑/↓`, `j/k` | Move selection in the annotations list |
| `PgUp/PgDn` | Scroll the annotations list by page |
| `Enter` | Jump to the selected annotation |
| configured delete key (`d` by default) | Delete the selected annotation |
| `Esc` | Cancel note editing or close the annotations list |

F2-F4 are fixed special keys. They are intentionally outside the printable keymap, so old `config.json` files cannot become invalid because of new default printable aliases.

## Durable model

Highlights use a half-open logical interval:

```text
Start ReadingLocation   inclusive
End ReadingLocation     exclusive
```

Both locations are in the same Domain block in M3.7 and use the existing UTF-16 `CharacterOffset` contract.

Personal notes use:

```text
ReadingLocation
Text
UpdatedUtc
```

The persisted snapshots additionally carry `BookPath` and `BookId`, exactly like bookmarks/history ownership.

## state.json schema 4

M3.7 adds two collections:

```json
{
  "schemaVersion": 4,
  "highlights": [
    {
      "path": "...",
      "bookId": "...",
      "start": { "sectionId": "...", "blockId": "...", "characterOffset": 10 },
      "end":   { "sectionId": "...", "blockId": "...", "characterOffset": 42 }
    }
  ],
  "notes": [
    {
      "path": "...",
      "bookId": "...",
      "location": { "sectionId": "...", "blockId": "...", "characterOffset": 10 },
      "text": "Nota personale",
      "updatedUtc": "2026-08-08T12:00:00+00:00"
    }
  ]
}
```

Schemas 1-3 load with empty annotation collections. The next save writes schema 4.

Never persisted:

- page number;
- visual line index;
- viewport dimensions;
- layout position;
- theme-specific colors;
- annotation overlay selection/scroll position.

## Rendering

The persisted highlight interval is exact and layout-independent. M3.7 deliberately renders at visual-line granularity: if a visual line intersects a stored logical range, that line is drawn with the theme's highlight attribute.

This keeps `EbookReader.Layout` format/persistence-neutral. A future milestone may add source-to-display character mapping for partial-line painting without changing the saved range format.

Theme behavior:

- Semantic dark: black text on yellow highlight;
- Paper light: black text on yellow highlight;
- Monochrome: black text on white highlight.

Outside highlighted lines, existing heading/strong/emphasis colors remain unchanged.

## Bounds

- 1,000 highlights overall;
- 250 highlights per book;
- 500 notes overall;
- 100 notes per book;
- 2,048 UTF-16 code units per note;
- 131,072 UTF-16 code units of note text overall;
- existing `state.json` maximum remains 1 MiB.

## Restore policy

An annotation is restored only when:

```text
full path matches
AND BookId matches
AND logical location(s) still exist in Book
```

When the current path is saved, all annotations previously associated with that same path are replaced. This also removes stale annotations if the EPUB at the path changed publication identity.
