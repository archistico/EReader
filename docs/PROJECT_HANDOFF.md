# Project Handoff — EReader M2.5

## Stato

- **Ultima baseline autoritativa validata:** M2.4 Hotfix 3 — Bookmark logici + semantic colors.
- Gate utente: `M2.4 HOTFIX 3 VALIDATION PASSED` con 395/395 casi.
- **Candidate corrente:** M2.5 — Stable Progress.
- M2.5 è costruita esclusivamente sopra `EReader_M2.4_Hotfix3_ArchitectureContract_NET10_Candidate.zip` validata.

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

Conteggio statico: 389 `[Fact]` + 16 `[InlineData]` = 405 casi attesi.

## Prossimo milestone

M3.0 / prossima voce roadmap da definire dopo validazione M2.5. La roadmap originaria prevedeva Library/History dopo il completamento del blocco M2.
