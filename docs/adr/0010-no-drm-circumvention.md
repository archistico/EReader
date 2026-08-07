# ADR-0010 — Non implementare aggiramento o rimozione DRM

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

EPUB può contenere risorse cifrate o essere distribuito con sistemi DRM. Il progetto è un reader didattico per contenuti liberamente leggibili dal file fornito.

## Decisione

EbookReader non implementa rimozione, bypass o gestione di DRM. Le risorse protette che impediscono la lettura normale vengono diagnosticate come non supportate. La gestione di eventuali meccanismi EPUB non-DRM tecnicamente cifrati/offuscati richiederà analisi specifica prima dell'implementazione.

## Conseguenze

- scope di sicurezza e interoperabilità più chiaro;
- errori espliciti anziché output corrotto;
- i test parser dovranno distinguere contenuto supportato e risorse protette/non supportate.

## Alternative considerate

### Supporto DRM

Fuori scope e non adottato.
