# Validation — M3.10 Hotfix 2 xUnit2031 Analyzer Alignment

M3.10 è costruita esclusivamente sopra la baseline autoritativa validata **M3.9 Hotfix 1** (`M3.9 HOTFIX 1 VALIDATION PASSED`, 08/08/2026).

Non modifica Domain, `ReadingLocation`, layout, state schema 4 o config schema 1. Il recovery resta confinato all'adapter EPUB/validator e alla presentazione diagnostica CLI.

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

## Criteri M3.10

- restore/build Release senza warning-as-error;
- suite completa xUnit;
- `CliEntryPoint.Milestone == "M3.10"`;
- help/foundation info coerenti con M3.10;
- tutti i gate M0–M3.9 continuano a passare;
- `EpubPackageReader.Read(...)` pubblico resta strict sulle risorse locali mancanti;
- `EpubPublicationValidator` può classificare risorse manifest mancanti dopo aver determinato il loro ruolo;
- Navigation Document/NCX assente o non utilizzabile non blocca un primary reading order valido;
- nessun TOC sintetico: recovery navigation usa `TableOfContents.Empty`;
- failure Container diverse da `EntryNotFound` durante la navigation restano `Invalid` + diagnostica Container;
- spine item primary mancante/non leggibile resta `Invalid`;
- spine item `linear="no"` con failure Content attesa può essere saltato con diagnostica recoverable;
- anchor di una sezione supplementare fallita non vengono committati parzialmente;
- immagine manifest referenziata ma fisicamente assente produce `MissingReferencedImage` e il `Book` resta leggibile;
- CSS/cover/risorsa locale non essenziale assente produce `Warning`;
- nessuna ricerca di fallback fuori dall'EPUB e nessun network fetch;
- CLI distingue `READABLE_DEGRADED`, `READABLE_WITH_WARNINGS` e `DOCUMENT_UNREADABLE`;
- una diagnostica navigation di recovery viene resa visibile solo se il `Book` finale è realmente leggibile;
- nessun catch-all per eccezioni interne inattese.

## Smoke M3.10 reale

Il gate include `test-books/m3.10-recovery-smoke.epub`.

Il file dichiara una navigation EPUB3 che non è presente nell'archivio, ma contiene un Content Document primary valido. Il comando:

```text
ereader --plain test-books/m3.10-recovery-smoke.epub
```

deve terminare con exit code `0`. La diagnostica attesa è recoverable/`READABLE_DEGRADED`; il testo primary deve essere renderizzato normalmente.

## Test count statico atteso

```text
485 Fact
5 Theory
19 InlineData
504 casi parametrizzati/non parametrizzati attesi
```

Il conteggio definitivo autoritativo resta quello stampato da `dotnet test` nel gate locale.

## Limite del gate

M3.10 dimostra recovery deterministico per navigation, risorse locali non essenziali e spine supplementare. Non implementa ancora:

- recovery granulare dei singoli link/fragment rotti — M3.11;
- crash containment di eccezioni interne EReader — M3.12;
- corpus sistematico di pubblicazioni corrotte con expected outcome — M3.13.


## Hotfix 1

Correzione esclusiva di CA2208 in `EpubPublicationValidator.AddContentRecoveryDiagnostics(...)`: il `paramName` di `ArgumentOutOfRangeException` è `nameof(issues)`. Nessuna modifica funzionale.


## Hotfix 2 — xUnit2031

Correzione esclusivamente nei test M3.10: le due selezioni diagnostiche usano `Assert.Single(collection, predicate)` al posto di `Assert.Single(collection.Where(predicate))`. Nessuna modifica funzionale.
