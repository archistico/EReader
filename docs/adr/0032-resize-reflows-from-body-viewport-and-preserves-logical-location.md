# ADR-0032 — Resize dal body viewport con preservazione della posizione logica

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

M1.1 ha introdotto un layout deterministico dipendente dal viewport e M1.2 ha separato la `ReadingLocation` logica dalle coordinate visuali effimere. M1.3 ha collegato questi contratti a Terminal.Gui, ma il viewport veniva stimato solo all'avvio.

Un resize può modificare wrapping, numero di righe, pagine e coordinate della stessa porzione di testo. Conservare `PageNumber` o `LineIndex` porterebbe quindi il lettore a un punto diverso del libro.

Inoltre la dimensione grezza della console non coincide necessariamente con lo spazio effettivo disponibile nel body della TUI, perché Window, border, header e footer consumano celle.

## Decisione

Durante l'esecuzione fullscreen:

1. `ReaderWindow` osserva `_body.ViewportChanged`, evento pubblico che Terminal.Gui 2.4.17 emette dopo aver aggiornato il viewport;
2. il viewport EPUB-reader viene ricavato dal `Viewport` reale del body;
3. `ReaderSession.Reflow(LayoutViewport)` ricostruisce il `BookLayout` se e solo se le dimensioni sono cambiate;
4. la `ReadingLocation` corrente viene preservata esattamente;
5. pagina e riga vengono nuovamente derivate tramite i resolver M1.2.

`Console.WindowWidth/Height` rimane soltanto una stima di bootstrap precedente al primo layout della TUI.

## Conseguenze

### Positive

- resize e reflow non alterano il punto logico di lettura;
- la UI usa la geometria reale concessa da Terminal.Gui;
- nessuna coordinata visuale entra nello stato durevole;
- `ReaderSession.Reflow` è unit-testabile senza Terminal.Gui;
- viewport invariati non provocano layout inutili.

### Negative

- ogni resize effettivo ricostruisce sincronicamente l'intero `BookLayout`;
- terminali estremamente piccoli richiedono clamp minimi di dimensione;
- il numero pagina mostrato può cambiare anche se il lettore non si è mosso, comportamento intenzionale.

## Alternative considerate

### Conservare pagina/riga

Rifiutata: sono coordinate del layout corrente e cambiano al reflow.

### Polling di `Console.WindowWidth/Height`

Rifiutato: duplica il lifecycle TUI e misura la console, non il body effettivo.

### Rendere Terminal.Gui responsabile del wrapping

Rifiutato: violerebbe ADR-0027 e duplicherebbe il layout engine.

### Debounce asincrono del resize

Rinviato: introduce complessità senza dati prestazionali che lo giustifichino.
