# ADR-0052 — Reader-wide diagnostics stay format-neutral

- **Status:** Accepted for M3.8 candidate
- **Date:** 2026-08-08

## Context

M0.7 possiede già una diagnostica stabile al boundary EPUB con `EpubValidationStatus.Valid`, `Invalid` e `Unsupported`. Dopo M3.7 il reader ha però più boundary operativi: stato, configurazione, navigazione, risorse e TUI. Riutilizzare direttamente i tipi `EbookReader.Epub.Validation` fuori dal composition root renderebbe Application dipendente dal formato sorgente e renderebbe difficile distinguere un documento irrecuperabile da un guasto interno EReader.

## Decision

M3.8 introduce in `EbookReader.Application.Diagnostics` una tassonomia format-neutral:

```text
ReaderDiagnosticSeverity
  Information
  Warning
  RecoverableError
  FatalDocumentError
  InternalError

ReaderOperationStatus
  Success
  SuccessWithDiagnostics
  DocumentUnreadable
  InternalFailure
```

Ogni `ReaderDiagnostic` espone codice stabile, area applicativa, messaggio umano, recovery dichiarata e contesto tecnico opzionale separato.

Il boundary EPUB M0.7 non viene modificato. `EbookReader.Cli.Diagnostics.EpubReaderDiagnosticBridge`, nel composition root che già conosce sia Application sia Epub, proietta `EpubDiagnostic` nel modello reader-wide.

Per l'attuale validator M0.7:

- `Valid` senza diagnostics → `Success`;
- `Valid` con diagnostics → `SuccessWithDiagnostics`;
- `Invalid` → `DocumentUnreadable`;
- `Unsupported` → `DocumentUnreadable`;
- `EpubDiagnosticSeverity.Error` su un risultato non valido/non supportato → `FatalDocumentError`.

Gli errori runtime inattesi non vengono catturati dal bridge né dal validator per trasformarli in errori documento. Il containment applicativo di tali errori appartiene a M3.12.

## Consequences

- `EbookReader.Application` resta indipendente da EPUB, layout e Terminal.Gui.
- M0.7 resta compatibile e autoritativo per l'ingestione.
- La CLI può comunicare chiaramente `DOCUMENT_UNREADABLE` senza perdere il codice EPUB specifico.
- `FatalDocumentError` significa fatale per il documento, non per il processo/applicazione.
- M3.9–M3.13 possono aggiungere nuove diagnostiche e recovery senza creare tassonomie parallele.
- M3.8 non implementa ancora catch-all, retry, repair, ZIP hardening aggiuntivo o degraded reading.

## Alternatives considered

### Estendere direttamente `EpubDiagnostic` a tutta l'applicazione

Rifiutato: introdurrebbe semantica EPUB nei layer che devono restare format-neutral.

### Sostituire `Valid/Invalid/Unsupported`

Rifiutato: romperebbe il contratto M0.7 già validato e confonderebbe formato/supporto con severità UX.

### Catturare tutte le eccezioni in M3.8

Rinviato a M3.12: un catch-all prematuro rischierebbe di mascherare bug EReader come documenti difettosi.
