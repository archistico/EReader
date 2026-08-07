# ADR-0022 — Nodi di navigazione di raggruppamento senza target

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Una gerarchia editoriale può contenere gruppi come "Parte I" che organizzano capitoli senza essere direttamente navigabili. EPUB 3 lo esprime con un `span` nel Navigation Document, ma il concetto non è specifico di EPUB.

Il modello M0.2 richiedeva un `ReadingLocation` per ogni `NavigationItem`, obbligando il mapper a perdere il gruppo o inventare una destinazione.

## Decisione

`NavigationItem.Target` diventa nullable. Un nodo con `Target = null` è valido solo se possiede almeno un figlio. `Book` valida la location soltanto quando il target esiste.

## Conseguenze

- la gerarchia editoriale viene preservata senza target sintetici;
- il Domain resta format-neutral;
- la futura TUI TOC dovrà distinguere nodi attivabili e nodi di puro raggruppamento.

## Alternative considerate

- target del primo figlio: respinto perché introduce semantica inventata;
- promuovere i figli eliminando il gruppo: respinto perché perde struttura;
- tipo EPUB-specifico nel Domain: respinto per ADR-0003.
