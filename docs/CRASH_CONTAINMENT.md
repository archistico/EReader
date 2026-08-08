# Crash Containment & Diagnostics UX

**Milestone:** M3.12 — candidate.  
**Baseline:** M3.11 Hotfix 1 VALIDATED (`M3.11 HOTFIX 1 VALIDATION PASSED`, 08/08/2026).

## Principio

> Un EPUB può essere illeggibile. EReader no.

M3.12 introduce il boundary runtime che completa la tassonomia M3.8. Un guasto inatteso del reader non viene mai riclassificato come `Invalid`/`Unsupported` EPUB e non modifica il contratto strict dei parser.

## Unico catch generale autorizzato

Il solo `catch (Exception ...)` intenzionale nei sorgenti produttivi vive in `CliEntryPoint.Run(...)`, cioè nel composition root. È accompagnato dalla soppressione documentata di CA1031 e da un exception filter.

Sono esclususe dal containment ordinario le failure process-critical:

- `OutOfMemoryException`;
- `StackOverflowException`;
- `AccessViolationException`.

Queste eccezioni non vengono presentate come normale `InternalFailure` perché il processo può non essere in uno stato affidabile per continuare.

`EpubPublicationValidator`, Container, OPF, Navigation e Content continuano a catturare soltanto failure attese e tipizzate. In particolare, un bug non diventa mai `Invalid EPUB` per effetto di M3.12.

## Outcome interno

Un'eccezione inattesa contenibile viene proiettata in:

```text
ReaderOperationStatus.InternalFailure
ReaderDiagnosticSeverity.InternalError
ReaderRecoveryAction.AbortOperation
ER-READER-INTERNAL-001
exit code 6
```

`InternalFailure` richiede almeno un `InternalError` e vieta `FatalDocumentError`: errore interno e documento irrecuperabile restano categorie semanticamente separate.

## UX predefinita

Senza opzioni diagnostiche, stderr mostra un messaggio breve:

```text
[INTERNAL ER-READER-INTERNAL-001] EReader ha rilevato un errore interno inatteso durante l'operazione corrente.
[INTERNAL_FAILURE] EReader ha interrotto l'operazione corrente per un errore interno inatteso.
Questo errore non dimostra che l'EPUB sia illeggibile. Se l'operazione riguardava un EPUB, il file non viene modificato; lo stato di lettura già salvato resta il riferimento persistente.
Ripeti il comando con --diagnostics per mostrare i dettagli tecnici.
```

Lo stack trace non è quindi il messaggio primario.

## Dettagli tecnici opzionali

`--diagnostics` è un modifier globale e può accompagnare, per esempio:

```text
ereader --diagnostics libro.epub
ereader --diagnostics --plain libro.epub
ereader --diagnostics --resume
```

In caso di `InternalFailure` aggiunge una sezione:

```text
[TECHNICAL_DETAILS ER-READER-INTERNAL-001]
<System.Exception.ToString()>
```

Il dettaglio comprende tipo eccezione, messaggio, inner exception e stack trace quando disponibili. Non cambia l'esito del comando quando non si verifica un errore.

## Preservazione dello stato

M3.12 non introduce snapshot di layout o rollback inventati. Si appoggia ai contratti già validati:

- `state.json` viene scritto solo al commit finale della sessione interattiva;
- `JsonReadingStateStore.Save(...)` usa temp file, flush-to-disk e rename same-directory;
- se `TerminalGuiReaderHost.Run(...)` o la preparazione del nuovo snapshot falliscono prima del commit, il vecchio stato persistente non viene sostituito;
- bookmark, highlight e note vengono ricostruiti in memoria e committati insieme allo snapshot finale;
- un EPUB `Invalid`/`Unsupported` non entra mai nel percorso di save;
- un link interno fallito resta già un no-op transazionale da M3.11.

Il messaggio M3.12 parla quindi di **stato già salvato** come riferimento persistente: non promette di salvare mutazioni in-memory appartenenti a un'operazione che è appena fallita.

## Errori attesi esclusi dal catch generale

Restano gestiti dai boundary specifici già esistenti:

- file non trovato → usage/path error;
- accesso/lettura file EPUB → errori I/O espliciti;
- EPUB invalido/non supportato → `DocumentUnreadable` con exit code 3/4;
- risorsa/TOC/link recuperabile → `SuccessWithDiagnostics`;
- errori previsti di stato/configurazione → diagnostiche `ER-STATE-*` / `ER-CONFIG-*`.

Il catch M3.12 è l'ultima rete, non il normale meccanismo di controllo del flusso.

## Non incluso

M3.12 non implementa ancora:

- log file persistente o telemetry;
- upload automatico di crash report;
- restart automatico della TUI;
- recovery dopo process-critical failures;
- corpus esteso di input corrotti, che resta M3.13.

## ADR

Vedi [`adr/0056-unexpected-failures-are-contained-only-at-composition-root.md`](adr/0056-unexpected-failures-are-contained-only-at-composition-root.md).
