# ADR-0008 — Eseguire la ricerca sul contenuto logico prima del layout

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Se la ricerca opera sulle righe renderizzate, il wrapping può spezzare una frase e la giustificazione può inserire spazi visuali che alterano il match.

## Decisione

Indicizzazione e ricerca lavorano su una rappresentazione testuale normalizzata derivata dal contenuto semantico. Il risultato è una location logica, poi proiettata sul layout per navigazione/highlight.

## Conseguenze

- risultati stabili al resize;
- giustificazione e temi non influenzano la ricerca;
- serve una mappatura affidabile tra testo normalizzato e location;
- l'highlight è responsabilità del layout/presentation, non del motore di ricerca.

## Alternative considerate

### Ricerca sulle righe renderizzate

Scartata perché lega il comportamento alla viewport.
