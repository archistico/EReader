# Project Handoff — EReader M2.5

## Stato

- **Ultima baseline autoritativa validata:** M2.5 — Stable Progress.
- Gate utente: `M2.5 VALIDATION PASSED`.
- **Baseline autoritativa validata:** M3.0 — Library & Reading History.
- **Candidate corrente:** M3.1 Hotfix 1 — Library Search false-positive fix.
- M3.0 è stata validata dall’utente. M3.1 ha compilato ma il gate ha fallito 2/422 test per falsi positivi fuzzy del path completo; Hotfix 1 corregge esclusivamente tale policy di matching e aggiunge un regression test.

## M2.5

M2.5 introduce `EbookReader.Application.Progress`:

- `BookProgress` — consumed/total logical units + fraction/percentage;
- `BookProgressIndex` — indice precomputato per `Book.ReadingOrder`;
- unità = `ContentText.GetPlainText(block).Length` code unit UTF-16;
- location = somma dei blocchi precedenti + `ReadingLocation.CharacterOffset`;
- supplementary incluse perché appartengono al reading order;
- libro senza testo logico → `0.0%`;
- fine ultimo blocco testuale → `100.0%`.

`ReaderSession` costruisce l'indice una sola volta e espone `Progress`. L'header normale mostra contemporaneamente:

```text
Cap. x/y   Pag. p/n   37.4%
```

La pagina resta effimera; la percentuale è logica e stabile.

## Invarianti

- nessun riferimento a `BookLayout`, `PageNumber`, `Viewport` nel modulo Progress;
- nessun `Progress`/`Percentage` persistito nello state JSON;
- resize/reflow della stessa `ReadingLocation` non modifica la percentuale;
- Domain/Epub/Layout non devono conoscere il calcolo percentuale;
- M2.4 bookmark, ricerca, colori e persistenza restano invariati.

## Gate

```text
.\validate.cmd
```

Esito atteso:

```text
M2.5 VALIDATION PASSED
```

Conteggio M2.5 validato: 405 casi.

## Prossimo milestone

M3.0 Library/History è validata. M3.1 implementa ora ricerca/filtro fuzzy transiente della libreria senza cambiare schema JSON.


## Checkpoint M3.1

- Baseline di partenza: M2.5 VALIDATED.
- Baseline: M3.0 Library & Reading History — VALIDATED.
- Candidate: M3.1 Library Search.
- `state.json` schema 3, massimo 200 history entry.
- `--library` TUI e `--history` plain.
- M3.0 resta baseline autoritativa finché M3.1 Hotfix 1 non supera il gate.
