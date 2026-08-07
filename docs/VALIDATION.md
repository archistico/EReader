# Validation — M3.0 Library & Reading History

## Baseline

M2.5 è la baseline autoritativa validata. L'utente ha eseguito il gate completo con esito:

```text
M2.5 VALIDATION PASSED
```

M3.0 è costruita esclusivamente sopra quella baseline.

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


## Criteri M3.0

- schema JSON corrente = 3, lettura compatibile schema 1/2;
- massimo 200 voci history ordinate per `LastOpenedUtc` discendente;
- una sola voce per path, sostituita quando cambia BookId/contenuto;
- ogni history entry conserva `ReadingLocation`, mai pagina/riga/viewport/progresso;
- apertura diretta di un libro recente ripristina la sua location se BookId e location sono validi;
- `--resume` resta semantica ultimo libro globale;
- `--history` è non interattivo e non modifica lo stato;
- `--library` usa Terminal.Gui solo nel CLI e non introduce dipendenze UI in Application;
- file mancanti restano visibili come `[mancante]` ma non vengono aperti;
- scrittura atomica e limite state.json da 1 MiB invariati.

## Suite attesa

```text
397 Fact
4 Theory
16 InlineData
----------------
413 casi attesi
```

## Esito atteso

```text
M3.0 VALIDATION PASSED
```


## M3.0 — Library & Reading History — CANDIDATE

Gate: `./validate.cmd` / `./validate.sh`. Devono passare restore, build warnings-as-errors, suite completa e smoke CLI. Esito atteso: `M3.0 VALIDATION PASSED`. Verificare inoltre `ereader --history` e manualmente `ereader --library` dopo avere aperto almeno due EPUB.
