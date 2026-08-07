# Project Handoff — EReader M2.3 Hotfix 1

## Stato corrente

- **Ultima baseline autoritativa validata:** M2.2 — Metadata View.
- **Gate baseline:** `M2.2 VALIDATION PASSED`.
- **Candidate corrente:** M2.3 Hotfix 1 — Search pre-layout analyzer fix.
- **Target:** .NET 10 / C# 14 / Terminal.Gui 2.4.17.

M2.3 era costruita esclusivamente sopra `EReader_M2.2_MetadataView_NET10_Candidate.zip` validata dall'utente. Hotfix 1 parte esclusivamente dalla candidate M2.3 testata dall'utente e corregge soltanto CA1861 nel test dei match sovrapposti.

## Contratti autoritativi invariati

- solo EPUB 2/3 reflowable senza DRM;
- Domain format-neutral;
- `ReadingLocation = SectionId + BlockId? + offset UTF-16` unica posizione durevole;
- layout e coordinate pagina/riga effimeri;
- resize sulla stessa `ReadingLocation`;
- persistenza JSON atomica senza coordinate visuali;
- Terminal.Gui confinato al CLI/TUI;
- AngleSharp confinato all'adapter EPUB Content;
- nessuna estrazione ZIP, rete o circumvention DRM;
- ADR autoritativi.

## M2.3 Hotfix 1

Nuovi componenti/contratti:

- `BookTextSearch` — ricerca bounded sul testo logico Domain;
- `BookSearchMatch` — `ReadingLocation` + lunghezza UTF-16;
- `BookSearchResultSet` — query, match e flag `IsTruncated`;
- stato ricerca effimero in `ReaderSession`;
- prompt inline `/` nella status bar;
- `n/N` per risultato successivo/precedente.

Comandi:

```text
/                   apre il prompt
Enter               cerca
Backspace           elimina ultimo grapheme
Esc                 annulla
n / N               risultato successivo / precedente
```

La ricerca è indipendente da wrapping e viewport e non viene persistita in `state.json`.

## Validation

```bat
.\validate.cmd
```

Gate atteso:

```text
M2.3 HOTFIX 1 VALIDATION PASSED
```

Conteggio statico: **358 Fact + 16 InlineData = 374 casi attesi**.

## ADR/documentazione

- ADR-0039 — La ricerca opera sul testo logico Domain prima del layout;
- `docs/SEARCH.md`.

## Prossimo passo dopo il PASS

**M2.4 — Bookmark logici**: bookmark persistiti come `ReadingLocation`, gestione add/remove/lista e salto dalla TUI senza dipendenza da pagina o layout.
