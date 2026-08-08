# ADR-0057 — Reliability is gated by a versioned on-disk EPUB corpus

- Status: Accepted for M3.13 candidate
- Date: 2026-08-08

## Context

Unit fixture generate in memoria verificano bene parser e invarianti locali, ma non dimostrano da sole che il prodotto continui a classificare correttamente file EPUB reali, corrotti e security-sensitive attraverso l'intera facade di ingestione.

## Decision

M3.13 mantiene sotto `test-books/reliability/` un corpus versionato di EPUB reali su disco con outcome e diagnostic code attesi. La suite apre quei file attraverso `EpubPublicationValidator`; una selezione viene inoltre attraversata dal CLI `--plain`.

Il corpus include sia casi leggibili/degradati sia rejection cases. Le fixture patologiche restano piccole: per esempio l'oversized entry usa metadata ZIP sintetici invece di distribuire centinaia di MiB di payload.

## Consequences

- regressioni su ZIP/OCF/OPF/content/recovery diventano riproducibili da file;
- i diagnostic code diventano parte del reliability contract;
- `INTERNAL_FAILURE` resta distinto dai document failure;
- il corpus sintetico non sostituisce il compatibility corpus M5.1.
