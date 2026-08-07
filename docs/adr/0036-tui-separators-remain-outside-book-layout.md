# ADR-0036 — I separatori TUI restano fuori dal BookLayout

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

La TUI mostra header, contenuto e status bar. Con M2.0 Hotfix 1 lo scorrimento per riga è diventato visibile, ma le tre aree restano poco distinguibili a colpo d'occhio. Servono due separatori orizzontali: uno sotto titolo/capitolo/pagina e uno sopra la status bar dei tasti.

## Decisione

`ReaderWindow` aggiunge due `Label` puramente visuali contenenti il carattere `─`. Il body viene posizionato fra i separatori e il suo `Viewport` effettivo continua a determinare il reflow. I separatori non entrano nel `BookLayout`, non generano `VisualLine` e non modificano la `ReadingLocation`.

## Conseguenze

La TUI distingue chiaramente header, area di lettura e comandi. Due righe terminale vengono dedicate al chrome, quindi il body ha due righe in meno rispetto alla Hotfix 1; il layout viene automaticamente rigenerato usando la nuova altezza reale del body.

## Alternative considerate

- **Usare un widget separator specifico di Terminal.Gui:** evitato per non introdurre un'altra dipendenza dalla surface API della versione 2.4.17 per un elemento puramente decorativo.
- **Disegnare i separatori nel testo del libro:** scartato perché contaminerebbe la proiezione del contenuto e le coordinate del layout.
- **Affidarsi solo al bordo esterno della Window:** non separa visivamente header e status bar dal testo.
