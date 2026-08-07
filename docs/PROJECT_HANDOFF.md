# Project Handoff — EReader M2.5

## Stato

- **Ultima baseline autoritativa validata:** M2.5 — Stable Progress.
- Gate utente: `M2.5 VALIDATION PASSED`.
- **Candidate corrente:** M3.0 — Library & Reading History.
- M3.0 è costruita esclusivamente sopra `EReader_M2.5_StableProgress_NET10_Candidate.zip` validata.

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

M3.0 implementa ora Library/History sopra la baseline M2.5 validata; M3.1 resta pianificata per ricerca/filtro della libreria.


## Checkpoint M3.0

- Baseline di partenza: M2.5 VALIDATED.
- Candidate: M3.0 Library & Reading History.
- `state.json` schema 3, massimo 200 history entry.
- `--library` TUI e `--history` plain.
- M2.5 resta baseline autoritativa finché M3.0 non supera il gate.
