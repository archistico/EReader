# ADR-0046 — I temi del reader mappano ruoli semantici solo nel boundary TUI

- **Stato:** Accepted
- **Data:** 2026-08-08

## Contesto

M2.4 ha introdotto ruoli visuali format-neutral nel layout (`Heading`, `Strong`, `Emphasis`, `StrongEmphasis`) e una palette Terminal.Gui concreta. M3.2 deve permettere di cambiare aspetto senza trasformare colori o preferenze UI in semantica Domain/Layout.

## Decisione

EReader definisce temi esclusivamente in `EbookReader.Cli.Tui`.

Ogni tema associa gli stessi ruoli semantici a `Terminal.Gui.Drawing.Attribute` e `Scheme` differenti. Il layout continua a produrre solo `VisualTextStyle`/`VisualTextSpan`.

M3.2 offre tre temi transitori:

1. **Semantico scuro** — palette M2.4: testo bianco, heading cyan, strong verde, emphasis giallo, chrome grigio su nero;
2. **Carta chiara** — fondo bianco, testo nero, heading cyan bold, strong verde bold, emphasis nero italic;
3. **Monocromatico** — bianco/grigio su nero, con distinzione tramite Bold/Italic.

Il tasto `c` cicla i temi nella sessione corrente. La scelta non viene salvata in `state.json`.

## Conseguenze

- Domain, EPUB, Application e Layout non conoscono temi o colori;
- ricerca, bookmark, progresso e `ReadingLocation` non cambiano;
- overlay, header, footer, separatori e body cambiano insieme;
- M3.3 potrà introdurre una configurazione separata e, se opportuno, rendere persistente la scelta tema.

## Alternative considerate

### Salvare subito il tema in `state.json`

Rifiutato: `state.json` contiene stato di lettura/libreria, non preferenze UI.

### Hardcodare tre set di colori direttamente in `ReaderWindow`

Rifiutato: mescolerebbe key handling e definizione della palette.

### Cambiare i colori dentro `EbookReader.Layout`

Rifiutato: violerebbe il boundary format/terminal-neutral stabilito dagli ADR-0003, ADR-0005 e ADR-0041.
