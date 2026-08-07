# ADR-0009 — Usare JSON per la persistenza locale iniziale

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Le prime necessità persistenti saranno impostazioni, libri recenti, ultima posizione e bookmark. Non richiedono query relazionali complesse.

## Decisione

Usare file JSON versionati come prima strategia di persistenza, con scrittura atomica tramite file temporaneo e replace/rename quando possibile.

## Conseguenze

- stato leggibile e facilmente diagnosticabile;
- nessuna dipendenza database nelle prime milestone;
- sarà necessario versionare lo schema dei file;
- se il volume o le query cresceranno, un ADR potrà introdurre SQLite con migrazione esplicita.

## Alternative considerate

### SQLite dall'inizio

Scartato come infrastruttura prematura.

### File binari proprietari

Scartati perché meno ispezionabili e senza vantaggi attuali.
