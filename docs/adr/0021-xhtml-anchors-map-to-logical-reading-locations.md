# ADR-0021 — Anchor XHTML → ReadingLocation logica

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

TOC e hyperlink EPUB usano path + fragment XHTML, ma il reader deve salvare, cercare e navigare senza dipendere dal file sorgente o dal wrapping terminale.

## Decisione

Durante M0.6 ogni `id` XHTML viene registrato come `ReadingLocation` format-neutral:

- block anchor → `SectionId + BlockId + offset 0`;
- inline anchor → `SectionId + BlockId + offset UTF-16`;
- anchor sul `body` → inizio sezione.

Gli offset seguono la proiezione testuale normalizzata che alimenta `ContentText`. Fragment percent-encoded vengono decodificati prima della risoluzione. Anchor duplicati e fragment mancanti sono errori strutturati.

## Conseguenze

- TOC e link restano stabili dopo resize e reflow;
- M1.x può costruire il layout senza conoscere id XHTML;
- bookmark e ricerca potranno usare lo stesso sistema di coordinate logiche;
- il parser deve fare almeno due fasi: raccolta anchor, poi risoluzione dei link/TOC.

## Alternative considerate

- conservare `href`/fragment nel Domain: respinto perché format-specific;
- mappare ogni anchor solo all'inizio del blocco: respinto perché perde precisione per note e link inline;
- salvare numero pagina: respinto da ADR-0007.
