# M2.4 — Bookmark logici

M2.4 aggiunge bookmark persistenti senza introdurre coordinate di layout nello stato.

## Comandi TUI

```text
b                 aggiunge/rimuove il bookmark alla ReadingLocation corrente
B                 apre/chiude l'elenco bookmark
↑ / ↓ oppure j/k  seleziona una voce nell'elenco
PgUp / PgDn       scorre rapidamente l'elenco
Enter             salta al bookmark selezionato
d                 elimina il bookmark selezionato
Esc               chiude l'elenco
```

Quando la posizione corrente coincide esattamente con un bookmark, l'header mostra `★`.

## Identità persistita

Ogni bookmark JSON contiene soltanto:

```text
bookPath
BookId
ReadingLocation
  ├── SectionId
  ├── BlockId?
  └── CharacterOffset UTF-16
```

Non vengono persistiti pagina, riga, viewport, snippet o label TUI.

## Schema JSON

M2.4 porta `schemaVersion` a `2`:

```json
{
  "schemaVersion": 4,
  "lastBook": { "...": "..." },
  "bookmarks": [
    {
      "path": "D:\\Ebook\\book.epub",
      "bookId": "urn:uuid:...",
      "location": {
        "sectionId": "chapter-3",
        "blockId": "p-12",
        "characterOffset": 44
      }
    }
  ]
}
```

Lo schema 1 di M2.0 viene ancora letto; equivale a una libreria bookmark vuota.

## Restore

Un bookmark viene reso disponibile per il libro corrente solo se:

1. il path normalizzato coincide;
2. il `BookId` coincide;
3. `Book.ContainsLocation(bookmark.Location)` è vero.

Gli altri bookmark restano nel file di stato per gli altri EPUB.

## Limiti

- massimo 1.000 bookmark per libro;
- massimo 10.000 bookmark complessivi nel file;
- file di stato massimo 1 MiB;
- scrittura atomica invariata: temp same-directory, `Flush(true)`, rename/overwrite.
