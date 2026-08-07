# ADR-0005 — Confinare Terminal.Gui 2.x nel frontend

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Terminal.Gui è la tecnologia scelta per la TUI, ma layout del testo e logica di lettura non devono dipendere dal framework visuale.

## Decisione

Solo `EbookReader.Cli` può referenziare `Terminal.Gui`. Il package è centralizzato alla linea 2.x selezionata. I View devono consumare modelli/layout già calcolati e non diventare il motore di paginazione.

## Conseguenze

- test layout senza driver terminale;
- migrazione futura della TUI meno invasiva;
- Terminal.Gui può essere sfruttato per input, focus, finestre, dialoghi e rendering, non come domain model.

## Alternative considerate

### Terminal.Gui in Application/Layout

Scartato perché accoppierebbe logica riutilizzabile al toolkit.

### Console API manuale per tutto

Scartato: reinventerebbe focus, resize, input e composizione TUI già forniti dal framework.
