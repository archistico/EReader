# ADR-0030 — Terminal.Gui reader come outer adapter sottile

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

M1.1 e M1.2 hanno già definito layout deterministico, mapping `ReadingLocation`→layout e navigazione logica. M1.3 deve introdurre una TUI fullscreen senza duplicare tali regole dentro una `View` Terminal.Gui, altrimenti wrapping, paginazione e stato di lettura diventerebbero dipendenti dal toolkit UI.

Terminal.Gui 2.x usa inoltre un modello applicativo instance-based (`Application.Create()`, `IApplication`, `Run`) che è preferibile al legacy static application model. La documentazione v2 raccomanda di usare i contratti moderni e di confinare il toolkit alla UI.

## Decisione

La TUI è divisa in due parti:

- `ReaderSession` compone `Book`, `DeterministicLayoutEngine`, `LayoutNavigator` e `LogicalReadingNavigator`; conserva come stato soltanto una `ReadingLocation` logica;
- `ReaderWindow` è un adapter Terminal.Gui che presenta header/body/footer, traduce i tasti in chiamate alla sessione e richiede lo stop dell'applicazione;
- `TerminalGuiReaderHost` possiede il ciclo di vita `IApplication`: create, init, run e dispose automatico tramite `using`;
- nessun tipo Terminal.Gui entra in Domain, Application o Layout.

M1.3 usa un viewport iniziale derivato dalle dimensioni console. Il reflow dinamico su resize è esplicitamente rinviato a M1.4.

## Conseguenze

- la maggior parte del comportamento reader è testabile senza driver terminale;
- `ReaderWindow` non implementa parsing EPUB, wrapping, paginazione o semantica di capitolo;
- cambiare toolkit TUI non richiederebbe di riscrivere il reader state;
- M1.4 potrà sostituire il layout della `ReaderSession` durante resize mantenendo la stessa `ReadingLocation`;
- coordinate pagina/riga restano effimere e non vengono persistite.

## Alternative considerate

### Mettere tutta la logica nella Window

Respinto: crea accoppiamento diretto tra comportamento di lettura e Terminal.Gui e duplica responsabilità già presenti in Application/Layout.

### Usare TextView come motore di scrolling

Respinto: lo scrolling nativo del widget introdurrebbe un secondo modello di posizione indipendente da `BookLayout` e `ReadingLocation`.

### Migrare layout e navigazione nel progetto CLI

Respinto: indebolirebbe i boundary già validati in M1.1/M1.2.
