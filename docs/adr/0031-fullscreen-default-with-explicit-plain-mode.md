# ADR-0031 — Reader fullscreen predefinito e modalità `--plain` esplicita

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

M1.0 ha introdotto `ereader <libro.epub>` come proiezione lineare su stdout. Con M1.3 il prodotto diventa realmente un ebook reader interattivo, ma la proiezione plain resta utile per scripting, debugging, pipe e regression smoke.

Avviare una TUI quando stdin/stdout sono rediretti produce inoltre un contratto ambiguo e può bloccare pipeline automatiche.

## Decisione

Da M1.3:

```text
ereader <libro.epub>          → Terminal.Gui fullscreen reader
ereader --plain <libro.epub>  → proiezione M1.0 su stdout
```

La TUI richiede stdin e stdout interattivi. Se sono rediretti, EReader restituisce un errore d'uso e indica `--plain`.

Il validation gate usa sempre `--plain` per lo smoke non interattivo. Gli exit code M1.0 restano invariati.

## Conseguenze

- il comando naturale apre il prodotto interattivo;
- l'output machine-friendly rimane disponibile e stabile;
- CI e script non rischiano di entrare nel main loop Terminal.Gui;
- stdout/stderr mantengono la separazione stabilita da ADR-0026.

## Alternative considerate

### Lasciare plain come default e introdurre `--tui`

Respinto: dopo M1.3 la funzione primaria del prodotto è la lettura interattiva, quindi il default deve rifletterla.

### Rimuovere la modalità plain

Respinto: è utile come diagnostica, smoke end-to-end e integrazione shell.
