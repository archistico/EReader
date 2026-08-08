# M3.8 — Diagnostics Foundation & Failure Taxonomy

**Stato:** foundation M3.8 VALIDATED; M3.9 security VALIDATED; M3.10 recovery CANDIDATE.  
**Baseline autoritativa corrente:** M3.9 Hotfix 1 VALIDATED.  
**Gate baseline:** `M3.9 HOTFIX 1 VALIDATION PASSED`.

## Principio guida

> Un EPUB può essere illeggibile. EReader no.

M3.8 introduce il linguaggio comune con cui i boundary EReader descrivono problemi e risultati. M3.9 ha validato il defensive input hardening. La candidate M3.10 usa lo stesso contratto per rendere distinguibili libro leggibile con warning, libro leggibile degradato e documento irrecuperabile; crash containment globale resta M3.12.

## Contratto M0.7 preservato

L'adapter EPUB continua a usare:

```text
EpubValidationStatus
  Valid
  Invalid
  Unsupported
```

`EpubPublicationValidator` continua a non catturare errori runtime inattesi e non cambia le sue eccezioni/codici diagnostici validati.

## Tassonomia reader-wide M3.8

Il nuovo namespace `EbookReader.Application.Diagnostics` è format-neutral e indipendente da EPUB, Layout e Terminal.Gui.

### ReaderDiagnosticSeverity

```text
Information
Warning
RecoverableError
FatalDocumentError
InternalError
```

- `Information`: informazione utile, nessuna degradazione.
- `Warning`: anomalia non bloccante.
- `RecoverableError`: problema reale per cui è stata applicata una recovery deterministica.
- `FatalDocumentError`: il documento non può proseguire in modo affidabile; non significa crash di EReader.
- `InternalError`: guasto inatteso del reader, distinto dai difetti del documento.

### ReaderOperationStatus

```text
Success
SuccessWithDiagnostics
DocumentUnreadable
InternalFailure
```

Invarianti implementate:

- `Success` non contiene diagnostics;
- `SuccessWithDiagnostics` richiede almeno una diagnostica e vieta fatal/internal errors;
- `DocumentUnreadable` richiede almeno un `FatalDocumentError` e nessun `InternalError`;
- `InternalFailure` richiede almeno un `InternalError`.

`CanContinue` è true soltanto per `Success` e `SuccessWithDiagnostics`.

## ReaderDiagnostic

Ogni diagnostica reader-wide contiene:

- `Code`: codice stabile machine-readable senza whitespace;
- `Severity`;
- `Area` format-neutral;
- `Message` umano;
- `RecoveryAction` dichiarata;
- `Resource` opzionale, come path virtuale o target logico;
- `TechnicalDetails` opzionali separati dal messaggio principale.

Non contiene pagina, riga o viewport.

### Aree

```text
Publication
Navigation
Content
Resource
Persistence
Configuration
Reader
```

### Recovery action

```text
None
Continue
ContinueDegraded
RejectDocument
KeepCurrentLocation
UseFallback
```

M3.8 definisce il vocabolario; le milestone successive useranno progressivamente le recovery pertinenti.

## Bridge EPUB → diagnostics applicativa

Il mapping vive in `EbookReader.Cli.Diagnostics.EpubReaderDiagnosticBridge` perché il CLI è il composition root autorizzato a conoscere sia `EbookReader.Epub` sia `EbookReader.Application`.

Mapping corrente:

```text
Epub Valid, nessuna diagnostica  -> Success
Epub Valid, con diagnostiche     -> SuccessWithDiagnostics
Epub Invalid                     -> DocumentUnreadable
Epub Unsupported                 -> DocumentUnreadable
```

Le categorie EPUB Container/Protection/Package vengono proiettate nell'area format-neutral `Publication`; Navigation e Content restano nelle omonime aree applicative.

Gli `EpubDiagnosticSeverity.Error` associati a `Invalid` o `Unsupported` diventano `FatalDocumentError` con `RejectDocument`.

## Output CLI M3.8

Una diagnostica irreversibile viene ora etichettata chiaramente, per esempio:

```text
[DOCUMENT-UNREADABLE ER-EPUB-CONTAINER-...] <dettaglio specifico>
[DOCUMENT_UNREADABLE] Impossibile aprire il libro in modo affidabile.
EReader ha rifiutato questo documento; il file EPUB non è stato modificato e lo stato di lettura esistente non viene aggiornato.
```

Gli exit code M1.0 restano compatibili: Invalid e Unsupported continuano a essere distinti a livello CLI.

## Cosa M3.8 non fa

- nessun nuovo catch-all globale;
- nessuna riclassificazione automatica delle eccezioni inattese come EPUB invalido;
- nessun repair ZIP/OPF/XHTML;
- nessuna recovery di risorse/capitoli;
- nessuna verifica HTTP dei link;
- nessuna modifica a `state.json` schema 4 o `config.json` schema 1;
- nessuna modifica a ReadingLocation, layout o annotazioni.

Questi punti sono distribuiti tra M3.9–M3.13.

## ADR

Decisione autoritativa: [`adr/0052-reader-wide-diagnostics-stay-format-neutral.md`](adr/0052-reader-wide-diagnostics-stay-format-neutral.md).

## Milestone successive

- M3.9 — Defensive EPUB Loading & Input Security
- M3.10 — EPUB Recovery & Degraded Reading
- M3.11 — Link Integrity & Navigation Security
- M3.12 — Crash Containment & Diagnostics UX
- M3.13 — Corrupted EPUB Corpus & Reliability Gate


## M3.10 — presentazione degraded

Un `EpubValidationStatus.Valid` con diagnostica EPUB `Error` viene proiettato dal bridge M3.8 in `RecoverableError` e `SuccessWithDiagnostics`. La CLI aggiunge un riepilogo `READABLE_DEGRADED`. Un `Warning` senza recovery error produce `READABLE_WITH_WARNINGS`.

Le diagnostiche navigation recuperabili vengono tenute provvisorie finché il `Book` non è costruito: se il primary content fallisce, non vengono mostrate promesse di continuazione contraddittorie.
