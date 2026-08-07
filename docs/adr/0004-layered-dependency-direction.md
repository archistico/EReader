# ADR-0004 — Imporre una direzione delle dipendenze verso il Domain

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Un'applicazione TUI rischia facilmente di concentrare parsing, stato, layout e rendering nel frontend. Questo rende difficile testare e cambiare UI.

## Decisione

Dipendenze produttive consentite:

- Domain → nessun altro progetto;
- Epub → Domain;
- Application → Domain;
- Layout → Domain;
- Cli → Domain + Epub + Application + Layout.

Il contratto è verificato da test architetturali.

## Conseguenze

- CLI è composition root e outer adapter;
- Domain, Application e Layout restano eseguibili/testabili senza terminale;
- eventuali nuove dipendenze inter-layer richiedono modifica intenzionale del contratto.

## Alternative considerate

### Un singolo executable project

Scartato perché non preserva i confini richiesti.

### Application dipendente da Epub

Scartato perché i casi d'uso devono operare su un libro già tradotto nel modello interno.
