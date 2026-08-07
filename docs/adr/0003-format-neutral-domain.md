# ADR-0003 — Rendere il Domain indipendente da EPUB

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

EPUB espone concetti tecnici come package document OPF, manifest, spine, itemref, NCX, nav document e XHTML. Se questi concetti entrano nel nucleo, parsing e lettura diventano accoppiati.

## Decisione

`EbookReader.Domain` descrive libri, metadata, reading order, navigazione, contenuto semantico, risorse e posizioni senza tipi o nomi EPUB-specifici. `EbookReader.Epub` traduce il formato nel Domain.

## Conseguenze

- test del dominio senza file EPUB;
- layout e application layer non conoscono ZIP/XML/XHTML;
- potenziale aggiunta futura di un altro input adapter senza riscrivere il reader;
- la conversione EPUB→Domain richiede una boundary esplicita.

## Alternative considerate

### `EpubBook` come modello centrale

Scartato perché farebbe trapelare il formato in ogni layer.

### DOM XHTML come modello interno

Scartato perché renderebbe layout, ricerca e bookmark dipendenti da AngleSharp e dalla struttura markup.
