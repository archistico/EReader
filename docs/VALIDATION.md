# Validation — M2.3 Hotfix 1 Search pre-layout

## Stato della catena

Ultima baseline autoritativa validata: **M2.2 — Metadata View** (`M2.2 VALIDATION PASSED`).

Candidate corrente: **M2.3 Hotfix 1 — Search pre-layout analyzer fix**.

Hotfix 1 è costruita esclusivamente sopra la candidate M2.3 che ha superato restore e quasi tutto il build Release, fermandosi solo su CA1861 nel test `BookTextSearchTests.SearchFindsOverlappingMatches`. Nessun codice produttivo è stato modificato.

## Gate

Da una estrazione pulita:

```bat
validate.cmd
```

oppure:

```sh
./validate.sh
```

Il gate esegue:

1. restore;
2. build Release con analyzer e warnings-as-errors;
3. suite completa;
4. smoke `--help`;
5. smoke `--version`;
6. smoke `--foundation-info`;
7. lettura end-to-end stateless di `test-books/m1.0-smoke.epub` con `--plain`.

Output finale atteso:

```text
M2.3 HOTFIX 1 VALIDATION PASSED
```

## Conteggio statico

- **358** `[Fact]`;
- **4** `[Theory]`;
- **16** casi `[InlineData]`;
- **374 casi attesi**.

## Criteri M2.3 Hotfix 1

Il PASS deve confermare, oltre a tutte le regressioni precedenti:

- il fix CA1861 usa un campo `static readonly` nel test senza sopprimere analyzer;
- nessun codice produttivo M2.3 cambia;
- M2.2 Metadata View resta funzionante e validata;
- `BookTextSearch` vive nell'Application layer e non referenzia Layout, Terminal.Gui o EPUB;
- la ricerca usa `ContentText.GetPlainText(ContentBlock)` e produce `ReadingLocation` Domain;
- confronto case-insensitive deterministico;
- match che attraversano container inline strong/emphasis/link restano trovabili;
- offset e lunghezze sono nello spazio UTF-16 Domain;
- match sovrapposti sono preservati;
- massimo 256 code unit UTF-16 per query;
- massimo 10.000 match con `IsTruncated`;
- `/` apre il prompt inline nella status bar;
- `Enter` esegue, `Backspace` elimina l'ultimo grapheme, `Esc` annulla;
- `n/N` navigano avanti/indietro con wrap-around;
- il primo risultato è il primo non precedente alla location corrente, con wrap quando necessario;
- nessun risultato cambia al reflow/resize;
- query e indice del risultato non entrano in `state.json`;
- `--plain` resta stateless.

## Evidenza precedente

- M0.1 Hotfix 2 — VALIDATED;
- M0.2 — VALIDATED;
- M0.3 Hotfix 1 — VALIDATED;
- M0.4 — VALIDATED;
- M0.5→M1.0 — VALIDATED;
- M1.1 — VALIDATED, 286/286;
- M1.2 Hotfix 1 — VALIDATED;
- M1.3 Hotfix 1 — VALIDATED;
- M1.4 Hotfix 2 — VALIDATED;
- M2.0 — VALIDATED;
- M2.0 Hotfix 1 — VALIDATED;
- M2.0 Hotfix 2 — VALIDATED;
- M2.1 — VALIDATED;
- M2.2 — `M2.2 VALIDATION PASSED`.
