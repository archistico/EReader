# Validation — M3.8 Hotfix 1 CA1859 Analyzer Alignment

M3.8 Hotfix 1 è costruita esclusivamente sopra la candidate M3.8 Diagnostics Foundation, a sua volta derivata dalla catena M3.7 Hotfix 1 validata + M3.7 Docs1 documentale. Non modifica il parser EPUB M0.7, i formati persistenti, ReadingLocation o il layout.

## Gate

Windows:

```bat
.\validate.cmd
```

Linux/macOS:

```sh
./validate.sh
```

Esito richiesto:

```text
M3.8 HOTFIX 1 VALIDATION PASSED
```

## Hotfix 1

La prima validazione M3.8 si è arrestata in build per `CA1859` in `ReaderOperationSummary.Validate(...)`. Il metodo è `private` e viene invocato esclusivamente con l’array già materializzato dal costruttore; la Hotfix 1 sostituisce quindi il parametro `IReadOnlyCollection<ReaderDiagnostic>` con `ReaderDiagnostic[]`. Nessuna semantica diagnostica, API pubblica o comportamento runtime viene modificato.

## Criteri M3.8

- restore/build Release senza warning/errori;
- suite completa attesa: 463 Fact + 4 Theory + 16 InlineData = 479 casi;
- `CliEntryPoint.Milestone == "M3.8"`;
- `EbookReader.Application.Diagnostics` non dipende da EPUB, Layout o Terminal.Gui;
- severità reader-wide: `Information`, `Warning`, `RecoverableError`, `FatalDocumentError`, `InternalError`;
- outcome: `Success`, `SuccessWithDiagnostics`, `DocumentUnreadable`, `InternalFailure`;
- invarianti di `ReaderOperationSummary` coperte da test;
- contratto M0.7 `Valid/Invalid/Unsupported` invariato;
- bridge EPUB localizzato nel composition root CLI;
- EPUB Invalid e Unsupported proiettati come `DocumentUnreadable`;
- output CLI di documento irrecuperabile contiene etichetta `DOCUMENT-UNREADABLE` e sommario `DOCUMENT_UNREADABLE`;
- exit code Invalid=3 e Unsupported=4 invariati;
- state schema 4 e config schema 1 invariati;
- smoke EPUB M1.0/M3.4/M3.5/M3.6 ancora passanti;
- nessun `bin/`, `obj/` o `graphify-out/` nel package candidato.

## Test mirati aggiunti

`ReaderDiagnosticsTests` verifica:

- contratto e campi di `ReaderDiagnostic`;
- codice machine-readable senza whitespace;
- normalizzazione del contesto opzionale;
- invarianti Success / SuccessWithDiagnostics;
- requisito fatal per DocumentUnreadable;
- requisito internal per InternalFailure;
- semantica `CanContinue`.

Il test architetturale M3.8 verifica che la tassonomia viva nell'Application layer e resti format/UI-independent.

I test CLI esistenti per EPUB invalido e cifrato verificano anche il nuovo output esplicito di documento illeggibile.

## Limite del gate

M3.8 non dimostra ancora crash containment per eccezioni runtime inattese. Il validator continua intenzionalmente a non nasconderle. Tale contratto verrà affrontato in M3.12 dopo l'hardening input/recovery delle milestone M3.9–M3.11.
