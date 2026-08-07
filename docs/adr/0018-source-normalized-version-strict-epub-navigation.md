# ADR-0018 — Navigazione EPUB normalizzata e selezione strict-by-version

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

EPUB 2 e EPUB 3 descrivono la navigazione primaria con meccanismi differenti: EPUB 2 usa NCX, mentre EPUB 3 usa un Navigation Document XHTML identificato nel manifest dalla proprietà `nav`. Il Domain di EReader non deve conoscere nessuno dei due formati.

## Decisione

`EbookReader.Epub` introduce un modello intermedio unico (`EpubNavigationDocument`, `EpubNavigationList`, `EpubNavigationNode`, `EpubNavigationTarget`) che non espone DOM XHTML o NCX.

La scelta della sorgente è strict-by-version:

- package EPUB 3 (`version="3.0"`) → esattamente un manifest item con `properties="nav"`;
- package EPUB 2 (`version="2.0"`) → NCX identificato da `spine/@toc`.

Non viene usato un NCX legacy per rendere accettabile un EPUB 3 privo del Navigation Document obbligatorio.

## Conseguenze

- il livello successivo può consumare una sola gerarchia di navigazione;
- i difetti di conformità non vengono nascosti da fallback permissivi;
- `nav.xhtml` e NCX rimangono dettagli dell'adapter EPUB;
- la conversione verso il TOC format-neutral del Domain resta una responsabilità successiva.

## Alternative considerate

### Conservare due modelli separati fino al Domain

Scartata: avrebbe propagato semantica EPUB nel nucleo.

### Fallback automatico EPUB 3 → NCX

Scartato: renderebbe leggibili come conformi package EPUB 3 che mancano del Navigation Document richiesto.
