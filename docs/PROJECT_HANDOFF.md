# Project Handoff — EReader M3.10 Hotfix 2 xUnit2031 Analyzer Alignment Candidate

Data: 08/08/2026

## Stato autoritativo

- baseline autoritativa validata: **M3.9 Hotfix 1 — Defensive EPUB Input Security + CA1859 Return-Type Alignment**;
- gate baseline: `M3.9 HOTFIX 1 VALIDATION PASSED`;
- candidate corrente: **M3.10 — EPUB Recovery & Degraded Reading**;
- gate candidate: `M3.10 HOTFIX 2 VALIDATION PASSED`.

## Obiettivo M3.10

Passare dal solo rifiuto sicuro dell'input difettoso a una recovery deterministica dei problemi non essenziali, mantenendo il principio:

> **Un EPUB può essere illeggibile. EReader no.**

M3.10 non deve mai inventare contenuto, cercare file sul filesystem, scaricare risorse remote o aggirare i guardrail M3.9.

## Decisioni implementate

### Navigation

- nav.xhtml/NCX assente o non utilizzabile → il validator tenta comunque il primary reading order;
- se il `Book` è valido, il risultato resta `Valid` con `TableOfContents.Empty` e diagnostica `ER-EPUB-RECOVERY-NAVIGATION-001`;
- se un TOC parsato non è risolvibile sul `Book` recuperato, viene eliminato interamente con `ER-EPUB-RECOVERY-NAVIGATION-002`;
- Container corruption/security durante la navigation resta fatal, salvo `EntryNotFound` della navigation stessa.

### Spine

- primary spine failure → documento `Invalid`;
- `linear="no"` + `EpubContentException` attesa → sezione saltata con `ER-EPUB-RECOVERY-CONTENT-001`;
- `SectionId` usa sempre l'indice originale dello spine;
- anchor per-sezione vengono committati solo dopo parsing completo.

### Risorse locali

`EpubPackageReader.Read(...)` pubblico resta strict. È stato aggiunto un percorso interno recovery-aware usato solo dalla facade di validation.

Dopo la costruzione del `Book`:

- immagine referenziata ma assente → `ER-EPUB-RECOVERY-RESOURCE-001`, severity EPUB `Error` → reader-wide `RecoverableError`;
- risorsa locale opzionale assente → `ER-EPUB-RECOVERY-RESOURCE-002`, `Warning`;
- risorse spine/navigation non ricevono diagnostiche duplicate.

### UX diagnostica

`ReaderDiagnosticTextWriter` aggiunge:

```text
[READABLE_DEGRADED] Il libro è leggibile, ma una o più parti non sono disponibili.
```

quando esiste almeno un `RecoverableError`, e:

```text
[READABLE_WITH_WARNINGS] Il libro è leggibile con avvisi non bloccanti.
```

quando esistono warning ma nessun errore recoverable.

`DOCUMENT_UNREADABLE` resta invariato per i documenti irreversibilmente rifiutati.

## Security invariants preservati

M3.10 non modifica i limiti M3.9:

- 100.000 ZIP entry;
- 256 MiB decompressi per entry;
- 2 GiB cumulativi dichiarati;
- ratio guard 500:1 sopra 16 MiB;
- symlink/special ZIP entries rifiutati;
- path traversal/drive/schema rifiutati;
- remote manifest solo http/https e nessun fetch;
- fallback OPF max 64;
- XHTML UTF-8/UTF-16 strict;
- nessun catch-all di eccezioni runtime arbitrarie.

## Gate

Windows:

```bat
.\validate.cmd
```

Linux/macOS:

```sh
./validate.sh
```

Esito atteso:

```text
M3.10 HOTFIX 2 VALIDATION PASSED
```

Il gate ha 13 step e include il nuovo `test-books/m3.10-recovery-smoke.epub` con navigation dichiarata ma mancante.

Conteggio statico candidate:

```text
485 Fact
5 Theory
19 InlineData
504 casi attesi
```

## File principali M3.10

Produzione:

- `src/EbookReader.Epub/Package/EpubPackageReader.cs`
- `src/EbookReader.Epub/Content/EpubBookReader.cs`
- `src/EbookReader.Epub/Content/EpubBookRecoveryResult.cs`
- `src/EbookReader.Epub/Validation/EpubPublicationValidator.cs`
- `src/EbookReader.Epub/Validation/EpubDiagnosticCodes.cs`
- `src/EbookReader.Cli/Diagnostics/ReaderDiagnosticTextWriter.cs`
- `src/EbookReader.Cli/CliEntryPoint.cs`

Test/gate:

- `tests/EbookReader.Epub.Tests/Validation/EpubPublicationValidatorTests.cs`
- `tests/EbookReader.Epub.Tests/Validation/ValidationFixtureFactory.cs`
- `tests/EbookReader.Cli.Tests/FirstReadableEpubTests.cs`
- `tests/EbookReader.Cli.Tests/FoundationSmokeTests.cs`
- `test-books/m3.10-recovery-smoke.epub`
- `validate.cmd`
- `validate.sh`

Decisione architetturale:

- `docs/adr/0054-degraded-reading-recovers-only-deterministic-nonessential-failures.md`

## Prossimo punto dopo il gate

**M3.11 — Link Integrity & Navigation Security**.

Obiettivo: rendere granulari i failure di hyperlink/anchor/noteref senza spostare `ReadingLocation` o back-stack in caso di target non valido; consolidare la allow-list degli schemi esterni e impedire qualsiasi handoff di `file:`, script/shell o schemi sconosciuti.


## M3.10 Hotfix 1

Build-only analyzer alignment: `EpubPublicationValidator.AddContentRecoveryDiagnostics(...)` usa `nameof(issues)` nel costruttore `ArgumentOutOfRangeException`, eliminando CA2208. Contratti e comportamento M3.10 restano invariati.


## Hotfix 2 analyzer alignment

Test-only: eliminati i due xUnit2031 in `EpubPublicationValidatorTests` usando l’overload predicate di `Assert.Single`. Produzione e contratti M3.10 restano invariati.
