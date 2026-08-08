# Project Handoff — EReader M3.8 Hotfix 1 CA1859 Analyzer Alignment Candidate

## Baseline

- baseline funzionale validata: `EReader_M3.7_Hotfix1_CompilationIntegration_NET10_Candidate.zip`;
- gate validato: `M3.7 HOTFIX 1 VALIDATION PASSED`;
- data validazione: 08/08/2026;
- documentazione consolidata: M3.7 Docs1 Reliability/Security Roadmap;
- candidate corrente: M3.8 Hotfix 1 — CA1859 Analyzer Alignment.

## Hotfix 1 — motivo

La prima candidate M3.8 ha fallito il build locale esclusivamente per `CA1859`: il metodo privato `ReaderOperationSummary.Validate(...)` dichiarava `IReadOnlyCollection<ReaderDiagnostic>` pur ricevendo sempre un `ReaderDiagnostic[]`. La Hotfix 1 usa il tipo concreto richiesto dall’analyzer; API pubblica e comportamento M3.8 restano invariati.

## Obiettivo M3.8

Formalizzare una tassonomia reader-wide prima dell'hardening concreto M3.9–M3.13, preservando il validator EPUB M0.7 già validato.

Principio:

> Un EPUB può essere illeggibile. EReader no.

## Implementazione

Nuovo namespace `EbookReader.Application.Diagnostics`:

- `ReaderDiagnosticSeverity`;
- `ReaderDiagnosticArea`;
- `ReaderRecoveryAction`;
- `ReaderDiagnostic`;
- `ReaderOperationStatus`;
- `ReaderOperationSummary`.

Tassonomia:

```text
Information
Warning
RecoverableError
FatalDocumentError
InternalError
```

Outcome:

```text
Success
SuccessWithDiagnostics
DocumentUnreadable
InternalFailure
```

Il modello è format-neutral e non contiene tipi EPUB, Terminal.Gui, layout o coordinate di pagina/riga/viewport.

## Compatibilità M0.7

`EpubPublicationValidator` e `EpubValidationResult` restano invariati.

Il nuovo `EpubReaderDiagnosticBridge` vive nel CLI/composition root:

```text
Valid + 0 diagnostics -> Success
Valid + diagnostics   -> SuccessWithDiagnostics
Invalid               -> DocumentUnreadable
Unsupported           -> DocumentUnreadable
```

`Invalid` e `Unsupported` mantengono i rispettivi exit code CLI 3 e 4.

## UX minima introdotta

Quando il validator rifiuta il documento, stderr contiene sia la diagnostica specifica sia un riepilogo esplicito:

```text
[DOCUMENT_UNREADABLE] Impossibile aprire il libro in modo affidabile.
```

Il messaggio dichiara inoltre che il file non viene modificato e che lo stato di lettura esistente non viene aggiornato.

## Contratti preservati

- Domain format-neutral;
- Application senza dipendenza EPUB/UI/Layout;
- `ReadingLocation` autoritativa e logica;
- state schema 4;
- config schema 1;
- M3.5 hyperlink/back-stack;
- M3.6 footnotes/endnotes;
- M3.7 highlight/note;
- M0.7 non cattura eccezioni runtime inattese.

## ADR

Nuovo ADR-0052: `reader-wide-diagnostics-stay-format-neutral`.

## Gate atteso

```bat
.\validate.cmd
```

Successo:

```text
M3.8 HOTFIX 1 VALIDATION PASSED
```

Conteggio statico atteso:

- 463 Fact;
- 4 Theory;
- 16 InlineData;
- 479 casi complessivi.

## Prossimo punto dopo validazione

**M3.9 — Defensive EPUB Loading & Input Security**.

M3.9 deve partire esclusivamente dalla M3.8 validata e concentrarsi sui guardrail dell'EPUB come input non attendibile: archive structure, path, limiti, XML/XHTML safety, risorse e URI. Non deve ancora diventare una milestone di recovery generale; quella resta M3.10.
