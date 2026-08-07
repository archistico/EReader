# Validation — M3.1 Hotfix 1 Library Search

## Baseline

- M3.0 — Library & Reading History: **VALIDATED**.
- M3.1 era costruita esclusivamente sopra `EReader_M3.0_LibraryReadingHistory_NET10_Candidate.zip` validata. Hotfix 1 è costruita esclusivamente sulla candidate M3.1 che ha compilato ma ha fallito 2/422 test per falsi positivi fuzzy generati dal path completo.

## Gate

Da una estrazione pulita:

```bat
.\validate.cmd
```

oppure:

```bash
./validate.sh
```

Il gate esegue restore, build Release con warnings-as-errors, suite completa, smoke CLI (`--help`, `--version`, `--foundation-info`, EPUB plain) e smoke `--history` con un `EREADER_STATE_FILE` temporaneo.

Esito atteso:

```text
M3.1 HOTFIX 1 VALIDATION PASSED
```

## Criteri M3.1

Devono risultare veri tutti i seguenti punti:

- `ReadingHistorySearch` vive in `EbookReader.Application.Library` e non dipende da Terminal.Gui;
- query vuota mantiene l'ordine cronologico M3.0;
- ricerca case-insensitive e accent-insensitive;
- ricerca su titolo, autore, nome file e path;
- sottosequenza fuzzy deterministica disponibile come fallback;
- match di titolo classificati prima di match equivalenti trovati solo nel path;
- query multi-token richiede un match per ogni token;
- query bounded a 128 code unit UTF-16;
- `/` apre il prompt filtro nella `LibraryWindow`;
- typing e Backspace aggiornano live i risultati;
- Backspace elimina l'ultimo grapheme, non un byte arbitrario;
- `Enter` applica il filtro e torna alla navigazione;
- `Esc` durante l'input annulla le modifiche e ripristina il filtro precedente;
- `Esc` con filtro attivo lo cancella prima di chiudere la libreria;
- `q` chiude sempre la libreria;
- `state.json` resta schema 3 e non contiene query/filtro libreria;
- Domain, EPUB e Layout restano indipendenti dalla funzione di ricerca libreria.

## Conteggio statico

- 406 `[Fact]`;
- 4 `[Theory]`;
- 16 `[InlineData]`;
- 423 casi attesi.

## Prova manuale suggerita

Dopo avere almeno due libri nella history:

```bat
ereader --library
```

Premere `/`, digitare una parte del titolo o dell'autore, verificare il filtro live, premere `Enter`, navigare con `j/k`, quindi `Esc` per cancellare il filtro.


## Hotfix 1

Il gate M3.1 originale ha compilato tutti i progetti ma ha fallito `SearchMatchesTitleCaseInsensitively` e `SearchRequiresEveryTokenToMatch`: il fuzzy-subsequence sul path completo poteva ricostruire token usando caratteri sparsi nelle directory temporanee. Hotfix 1 limita il path a exact/prefix/substring e mantiene il fuzzy solo su titolo, autore e nome file.
