# ADR-0044 — La libreria recente è stato JSON logico bounded

- **Stato:** Accepted
- **Data:** 2026-08-08

## Contesto

Dopo resume, bookmark e progresso, EReader deve poter riaprire più libri recenti senza introdurre un database o dipendere dal layout terminale.

## Decisione

La libreria M3.0 è una cronologia bounded, massimo 200 elementi, persistita nello stesso `state.json` atomico tramite schema 3. Ogni voce conserva path, BookId, metadata display minimi, LastOpenedUtc e ReadingLocation. Non conserva pagine, righe, viewport o percentuale derivata.

La selezione interattiva appartiene al CLI/Terminal.Gui; il modello e la policy di cronologia restano in Application e dipendono solo dal Domain.

## Conseguenze

- restore multi-book stabile rispetto al reflow;
- nessun database da migrare o amministrare;
- stato bounded e portabile;
- file spostati possono risultare stale e sono mostrati come mancanti;
- ricerca fuzzy rimandata a M3.1.

## Alternative considerate

- SQLite: eccessivo per il volume M3.0;
- scansione automatica di cartelle: introduce ownership e discovery fuori scope;
- persistere la percentuale: duplicazione di un dato derivato da M2.5.
