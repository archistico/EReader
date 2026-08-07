# M2.0 — Reading State JSON atomico

## Obiettivo

M2.0 rende persistente la posizione di lettura senza rendere persistente il layout.

La fonte di verità rimane:

```text
ReadingLocation
├── SectionId
├── BlockId?
└── CharacterOffset UTF-16
```

Non vengono salvati pagina, riga, viewport o wrapping.

## File di stato

Percorso predefinito:

```text
Environment.SpecialFolder.LocalApplicationData
└── EReader
    └── state.json
```

Su Windows corrisponde normalmente a `%LOCALAPPDATA%\EReader\state.json`.

Per test/uso portabile il percorso può essere sovrascritto con:

```text
EREADER_STATE_FILE=<percorso assoluto o relativo>
```

## Schema M2.0

Esempio:

```json
{
  "schemaVersion": 2,
  "lastBook": {
    "path": "D:\\Ebook\\libro.epub",
    "bookId": "urn:uuid:...",
    "lastOpenedUtc": "2026-08-07T20:00:00+00:00",
    "location": {
      "sectionId": "section-12",
      "blockId": "paragraph-41",
      "characterOffset": 183
    }
  }
}
```

`blockId` può essere assente quando la posizione è l'inizio logico di una sezione.

## Scrittura atomica

`JsonReadingStateStore.Save`:

1. crea la directory se non esiste;
2. crea un `.tmp` univoco nella stessa directory;
3. serializza il documento;
4. forza il flush su disco;
5. rinomina il temporaneo sopra `state.json`;
6. tenta di eliminare eventuali residui temporanei.

Il file è bounded a 1 MiB in lettura.

## Restore

Quando si apre esplicitamente un EPUB, EReader prova a riutilizzare lo snapshot solo se:

```text
same normalized path
AND same BookId
AND Book.ContainsLocation(savedLocation)
```

Altrimenti apre il libro dall'inizio.

Il comando:

```text
ereader --resume
```

riapre il percorso dell'ultimo libro persistito.

## Quando viene salvato

M2.0 salva dopo una chiusura pulita della TUI, quando `TerminalGuiReaderHost.Run` restituisce la `ReadingLocation` finale.

Un kill del processo o crash prima della chiusura può perdere gli ultimi movimenti della sessione; autosave periodico/event-driven è deliberatamente fuori M2.0.

## Plain mode

```text
ereader --plain libro.epub
```

non legge e non modifica lo stato persistente.

Questo mantiene la proiezione CLI deterministica per script, pipe e validation gate.


## M2.4 — Bookmark logici

Lo schema corrente è **2**. Oltre a `lastBook` contiene `bookmarks`, una libreria multi-book di path + BookId + ReadingLocation. Il loader continua ad accettare lo schema 1, interpretandolo come stato senza bookmark.

Nessun bookmark contiene pagina, riga, viewport o layout. Vedi [`BOOKMARKS.md`](BOOKMARKS.md).
