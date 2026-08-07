# ADR-0006 — Possedere il parser EPUB e confinare AngleSharp

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Il progetto ha valore didattico e vuole comprendere realmente container, package, spine e navigazione EPUB. Una libreria EPUB completa nasconderebbe gran parte del formato. XHTML, invece, è un problema di parsing markup già ben risolto.

## Decisione

Implementare direttamente la pipeline EPUB read-only usando API .NET per ZIP/XML e codice del progetto per le regole EPUB. Usare AngleSharp per il parsing XHTML, confinandolo a `EbookReader.Epub`. La decisione è attuata da M0.6 nel boundary `EbookReader.Epub.Content`; la versione viene gestita centralmente.

## Conseguenze

- pieno controllo e comprensione del formato;
- nessuna dipendenza del Domain dal DOM;
- maggiore responsabilità di test per URI, spine, navigation e malformed input;
- una libreria EPUB esterna può essere usata nei test come oracle, ma non come engine produttivo, se deciso separatamente.

## Alternative considerate

### Libreria EPUB completa come engine

Scartata come cuore del progetto perché riduce il controllo sul processo didattico e sui contratti interni.

### Parser HTML scritto da zero

Scartato perché non è il problema che il progetto vuole risolvere.
