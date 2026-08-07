# ADR-0007 — Persistire una posizione di lettura logica

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Il numero di pagina in un reader reflowable dipende da larghezza, altezza, stile e modalità di visualizzazione. Dopo un resize la stessa pagina numerica può indicare contenuto diverso.

## Decisione

La posizione autoritativa identifica un punto nel reading order e nel contenuto semantico, non una pagina visuale. Il dettaglio concreto del value object verrà definito in M0.2 e potrà includere section/block/offset o un equivalente stabile.

## Conseguenze

- resize e repagination preservano il punto del libro;
- bookmark e resume usano lo stesso concetto;
- la pagina visualizzata diventa una proiezione temporanea del layout;
- i test devono coprire round-trip location→layout→location.

## Alternative considerate

### Persistenza del numero pagina

Scartata perché instabile per definizione in contenuti reflowable.

### Persistenza della riga terminale

Scartata per lo stesso motivo e ancora più dipendente dalla viewport.
