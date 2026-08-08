# Validation — M3.9 Defensive EPUB Loading & Input Security

M3.9 è costruita esclusivamente sopra la baseline autoritativa validata M3.8 Hotfix 1. Non modifica Domain, `ReadingLocation`, layout, state schema 4, config schema 1 o i comandi di lettura esistenti.

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
M3.9 HOTFIX 1 VALIDATION PASSED
```

## Criteri M3.9

- restore/build Release senza warning-as-error;
- suite completa xUnit;
- `CliEntryPoint.Milestone == "M3.9"`;
- help/foundation info coerenti con M3.9;
- tutti i gate M0–M3.8 continuano a passare;
- path OCF drive-qualified, anche percent-encoded, e traversal restano rifiutati;
- ZIP entry Unix di tipo speciale (inclusi symbolic-link) rifiutata;
- compression ratio patologico rifiutato prima dell'uso della entry;
- budget individuale/cumulativo ZIP presenti e confinati all'adapter EPUB;
- risorse manifest remote soltanto `http`/`https`;
- fallback chain oltre 64 rifiutata;
- byte UTF-8 invalidi e control character XML in XHTML diventano `EpubContentException.InvalidXhtml`;
- compression method ZIP non supportato scoperto durante una entry produce `EpubValidationStatus.Invalid` + diagnostica Container;
- nessun `System.IO.Compression` / `ZipArchive` in Domain/Application;
- nessuna rete automatica o estrazione filesystem introdotta;
- nessun catch-all per eccezioni interne inattese.

## Test count statico atteso

```text
473 Fact
5 Theory
19 InlineData
492 casi parametrizzati/non parametrizzati attesi
```

Il conteggio definitivo autoritativo è comunque quello stampato da `dotnet test` nel gate locale.

## Limite del gate

M3.9 dimostra defensive loading e classificazione dei failure di input previsti. Non implementa ancora degraded reading/recovery di risorse o capitoli (M3.10), link integrity UX (M3.11) né crash containment di eccezioni interne inattese (M3.12).
