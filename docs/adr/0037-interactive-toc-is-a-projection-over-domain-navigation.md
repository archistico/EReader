# ADR-0037 — Interactive TOC come proiezione del Domain Navigation

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

Da M0.6 il `Book` format-neutral contiene un `TableOfContents` gerarchico i cui target sono `ReadingLocation`. M2.1 deve renderlo interattivo nella TUI senza reintrodurre concetti EPUB (`nav.xhtml`, NCX, href/fragment) e senza fare del widget Terminal.Gui la fonte di verità della navigazione.

Il TOC può inoltre contenere nodi puramente organizzativi con `Target == null` e figli navigabili.

## Decisione

La TUI usa una proiezione appiattita `ReaderTocEntry` che conserva:

- label;
- profondità gerarchica;
- `ReadingLocation?` target.

`ReaderSession` costruisce la proiezione dal `Book.TableOfContents`, individua la voce navigabile più vicina alla posizione corrente e applica i salti tramite `ReadingLocation`.

`ReaderWindow` mantiene soltanto stato effimero di presentazione (TOC aperto/chiuso, indice selezionato, scroll offset) e renderizza l'indice nello stesso body del reader. I nodi senza target restano visibili ma non selezionabili.

Non vengono introdotti `ListView`, `TreeView` o `Dialog`: M2.1 usa il body esistente per ridurre la dipendenza dalla surface API di Terminal.Gui e mantenere stabile il resize M1.4.

## Conseguenze

- EPUB e TOC TUI restano disaccoppiati;
- il salto dall'indice usa lo stesso spazio logico della persistenza M2.0;
- aprire/scorrere/chiudere il TOC non modifica la posizione di lettura;
- `Enter` su una voce navigabile aggiorna la `ReadingLocation` e torna al reader;
- la gerarchia editoriale è visibile tramite indentazione;
- la selezione salta automaticamente i grouping node senza target;
- resize del terminale non cambia la semantica del TOC né della posizione.

## Alternative considerate

### `TreeView` Terminal.Gui

Scartato per M2.1: più coupling alla libreria UI e maggiore surface API da stabilizzare senza aggiungere valore al modello di lettura.

### `ListView` con stringhe appiattite

Scartato: avrebbe duplicato comunque la logica di selezione/target e reso la navigazione dipendente dallo stato del widget.

### Conversione del TOC in capitoli EPUB-specifici

Scartata: violerebbe il contratto format-neutral del Domain.
