# ADR-0033 — Stato di lettura JSON versionato con sostituzione atomica

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

Dopo M1.4 EReader possiede una `ReadingLocation` logica stabile anche quando il viewport cambia. M2.0 deve conservarla tra esecuzioni senza introdurre un database, senza dipendere dal layout corrente e senza rischiare di lasciare un file di stato parzialmente scritto in caso di interruzione durante il salvataggio.

ADR-0009 aveva già scelto JSON come prima forma di persistenza.

## Decisione

EReader mantiene un solo documento `state.json` versionato, relativo all'ultimo libro aperto. Lo schema M2.0 contiene:

- `schemaVersion`;
- percorso assoluto dell'ultimo EPUB;
- `BookId` del `Book` format-neutral;
- `lastOpenedUtc`;
- `ReadingLocation` composta da `SectionId`, `BlockId?` e offset UTF-16.

Non vengono serializzati `PageNumber`, `LineIndex`, `LayoutPosition`, viewport o altre coordinate visuali.

Il salvataggio usa:

1. file temporaneo univoco nella stessa directory di `state.json`;
2. serializzazione JSON UTF-8;
3. `Flush(flushToDisk: true)`;
4. `File.Move(..., overwrite: true)` nella stessa directory;
5. cleanup best-effort del temporaneo residuo.

Il documento ha un limite di 1 MiB e versioni schema sconosciute vengono rifiutate.

## Conseguenze

### Positive

- la posizione persistita è indipendente da terminale e reflow;
- un crash durante la scrittura non produce intenzionalmente un mezzo documento di destinazione;
- il formato è leggibile e facilmente diagnosticabile;
- non servono dipendenze runtime aggiuntive;
- una futura migrazione schema è possibile tramite `schemaVersion`.

### Negative

- M2.0 conserva un solo ultimo libro, non una libreria completa;
- il salvataggio avviene alla chiusura pulita della TUI e non garantisce l'ultimo movimento in caso di kill/crash;
- la sostituzione dipende dalle garanzie di rename del filesystem locale.

## Alternative considerate

### SQLite

Rinviato: sproporzionato per un singolo snapshot e contrario alla decisione JSON-first di ADR-0009.

### Scrittura diretta su `state.json`

Rifiutata: una terminazione durante la serializzazione può lasciare JSON troncato.

### Persistenza della pagina corrente

Rifiutata: viola ADR-0007, ADR-0028, ADR-0029 e ADR-0032.
