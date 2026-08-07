# ADR-0034 — Resume solo con path, BookId e ReadingLocation ancora coerenti

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

Un file EPUB può essere sostituito mantenendo lo stesso nome, spostato, rigenerato oppure modificato internamente. Ripristinare ciecamente `SectionId`/`BlockId` da un vecchio stato potrebbe portare a un punto non più esistente o, peggio, a una posizione semanticamente diversa.

## Decisione

Una `ReadingLocation` salvata viene riutilizzata soltanto quando tutte le condizioni sono vere:

1. il percorso assoluto corrente corrisponde a quello persistito;
2. il `BookId` corrente corrisponde a quello persistito;
3. `Book.ContainsLocation(savedLocation)` è vero.

Se una condizione fallisce, il libro viene aperto normalmente dalla posizione iniziale e il vecchio snapshot non viene interpretato euristicamente.

Il comando `ereader --resume` usa il percorso dell'ultimo libro salvato. Se non esiste uno stato valido o il file non è più disponibile, il comando fallisce con una diagnostica esplicita invece di cercare file alternativi.

`ereader --plain` è stateless: non carica né modifica `state.json`.

## Conseguenze

### Positive

- nessun resume verso coordinate obsolete;
- nessuna dipendenza dal numero di pagina;
- sostituire un EPUB con un'altra pubblicazione non eredita accidentalmente la posizione precedente;
- `--plain` rimane deterministico e sicuro per pipe/smoke.

### Negative

- spostare manualmente lo stesso EPUB in un'altra directory perde il resume M2.0;
- M2.0 non implementa ancora fingerprint/hash del file o ricerca della pubblicazione spostata.

## Alternative considerate

### Confrontare solo il percorso

Rifiutata: un file può essere sostituito in-place.

### Confrontare solo BookId

Rifiutata per M2.0: non gestisce in modo esplicito il concetto di file spostato e può creare ambiguità tra copie.

### Correggere automaticamente SectionId/BlockId mancanti

Rifiutata: introdurrebbe euristiche non deterministiche nel core della posizione.
