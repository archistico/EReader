# ADR-0023 — Risultato di ingestione stabile e tassonomia diagnostica

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

M0.3–M0.6 espongono eccezioni specifiche per OCF, OPF, Navigation e Content. Una CLI non deve conoscere tutti questi dettagli né distinguere il tipo concreto di eccezione per comunicare correttamente un errore all'utente.

Serve inoltre distinguere un EPUB **malformato** da un EPUB riconoscibile ma **fuori scope**.

## Decisione

`EpubPublicationValidator` è la facade di ingestione supportata e restituisce `EpubValidationResult` con stato `Valid`, `Invalid` o `Unsupported`.

Quando `Valid`, il risultato contiene il `Book` format-neutral. Gli errori EPUB attesi vengono convertiti in `EpubDiagnostic` con codice machine-readable stabile, categoria, severità e messaggio.

Le eccezioni inattese e gli errori di programmazione non vengono catturati genericamente.

## Conseguenze

- la futura CLI dipende da un contratto semplice;
- test e scripting possono usare codici anziché confrontare testo localizzato;
- `Invalid` e `Unsupported` hanno semantica distinta;
- il Domain non acquisisce alcun tipo diagnostico EPUB.

## Alternative considerate

- catturare tutte le eccezioni nel CLI: respinto, accoppia UI e parser;
- usare solo messaggi testuali: respinto, contratto fragile;
- convertire anche errori runtime inattesi in diagnostiche EPUB: respinto, rischia di nascondere bug.
