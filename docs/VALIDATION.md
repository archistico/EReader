# Validation — M2.5 Stable Progress

## Baseline

M2.4 Hotfix 3 è la baseline autoritativa validata. L'utente ha eseguito il gate completo con esito:

```text
M2.4 HOTFIX 3 VALIDATION PASSED
```

M2.5 è costruita esclusivamente sopra quella baseline.

## Gate Windows

```bat
.\validate.cmd
```

## Gate manuale

```bat
dotnet restore EbookReader.sln
dotnet build EbookReader.sln -c Release --no-restore
dotnet test --solution EbookReader.sln -c Release --no-build
```

Seguono gli smoke CLI `--help`, `--version`, `--foundation-info` e `--plain test-books\m1.0-smoke.epub`.

## Criteri M2.5

- tutti i test M0→M2.4 restano verdi;
- `BookProgressIndex` usa solo `Book`, `ReadingLocation` e `ContentText` Domain;
- unità logica coerente con `CharacterOffset`: code unit UTF-16;
- inizio primo reading section = 0%;
- fine ultimo blocco testuale = 100%;
- supplementary incluse nel reading order;
- libro privo di testo logico = 0%;
- percentuale invariata alla stessa location dopo `ReaderSession.Reflow`;
- header normale mostra percentuale con una cifra decimale;
- nessun dato percentuale entra in `state.json`;
- nessuna dipendenza da pagina/riga/viewport nel modulo Progress.

## Suite attesa

```text
389 Fact
4 Theory
16 InlineData
----------------
405 casi attesi
```

## Esito atteso

```text
M2.5 VALIDATION PASSED
```
