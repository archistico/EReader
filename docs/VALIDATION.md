# Validation — M3.7 Hotfix 1 — Compilation Integration

La funzionalità M3.7 deriva esclusivamente dalla baseline validata M3.6 Hotfix 1. Hotfix 1 applica soltanto correzioni di compilazione/integrazione alla candidate M3.7 originale che ha fallito il primo build locale. La Hotfix 1 è stata successivamente validata dall'utente l'08/08/2026 ed è la baseline autoritativa corrente.

## Gate

Windows:

```bat
.\validate.cmd
```

Linux/macOS:

```sh
./validate.sh
```

Esito richiesto:

```text
M3.7 HOTFIX 1 VALIDATION PASSED
```

## Correzioni Hotfix 1

- test architetturale M3.7 dentro la classe xUnit corretta;
- helper `TemporaryDirectory` disponibile in `ReadingAnnotationTests`;
- namespace `EbookReader.Domain.Content` importato in `ReaderBodyView` per `BlockId`.

## Criteri M3.7

- restore/build Release senza warning/errori;
- suite completa: 454 Fact + 4 Theory + 16 InlineData = 470 casi attesi;
- `CliEntryPoint.Milestone == "M3.7"`;
- state schema corrente = 4 e loader compatibile 1/2/3;
- round-trip highlight/note senza page/line/viewport;
- restore/replace annotation book-scoped;
- F2/F3/F4 presenti nel TUI/help come special keys;
- `ReaderBodyView` applica highlight fuori dal Layout;
- `config.json` resta schema 1;
- smoke EPUB M1.0/M3.4/M3.5/M3.6 continuano a passare in `--plain`;
- smoke library/config non scrivono nei file reali dell'utente.

## Prova manuale consigliata

1. aprire un EPUB con la TUI;
2. premere F2 su una riga di testo e verificare il background highlight;
3. fare resize e verificare che il contenuto logico evidenziato resti associato al testo;
4. premere F3, inserire una nota, Enter;
5. premere F4, verificare `[E]` e `[N]`, Enter per saltare e `d` per eliminare;
6. uscire e riaprire lo stesso EPUB: highlight e nota devono essere ripristinati.
