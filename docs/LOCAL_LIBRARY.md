# M3.0 — Library & Reading History

## Obiettivo

EReader mantiene una piccola libreria locale dei libri realmente aperti, senza database e senza scansione automatica di directory.

## Comandi

```text
ereader --library
ereader --history
ereader --resume
```

`--library` apre una TUI fullscreen ordinata per ultimo accesso. Frecce o `j/k` selezionano, `PgUp/PgDn` scorrono, `Enter` apre e `q/Esc` chiude. I file non più presenti vengono marcati `[mancante]` e non vengono aperti.

`--history` emette lo stesso insieme come testo su stdout ed è adatto a terminali rediretti/script.

## Persistenza

`state.json` passa allo schema 3. Ogni voce contiene:

- path assoluto;
- `BookId`;
- titolo;
- autori format-neutral, se disponibili;
- `LastOpenedUtc`;
- `ReadingLocation`.

Non vengono persistiti pagina, riga, viewport, `BookLayout` o percentuale di avanzamento. Il massimo è 200 voci; la scrittura atomica M2.0 resta invariata.

## Resume multi-book

`--resume` continua ad aprire l'ultimo libro globale. Aprendo invece direttamente un EPUB già presente nella cronologia, EReader usa la voce corrispondente per ripristinare la sua `ReadingLocation`, purché path, BookId e location siano ancora validi.

## Compatibilità

Gli schema 1 e 2 vengono ancora letti. Non avendo una lista `history`, il loro `lastBook` viene rappresentato come prima voce della cronologia usando il nome file come titolo di fallback.

## Fuori scope

- scansione di directory;
- import di librerie Calibre;
- database;
- fuzzy search (M3.1);
- copertine/thumbnail.
