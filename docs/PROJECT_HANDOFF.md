# Project Handoff — EReader M2.2

## Stato corrente

- **Ultima baseline autoritativa validata:** M2.1 — Interactive TOC.
- **Gate baseline:** `M2.1 VALIDATION PASSED`.
- **Candidate corrente:** M2.2 — Metadata View.
- **Target:** .NET 10 / C# 14 / Terminal.Gui 2.4.17.

M2.2 è costruita esclusivamente sopra `EReader_M2.1_InteractiveTOC_NET10_Candidate.zip` validata dall'utente.

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

## M2.2

Nuovi componenti/contratti:

- `ReaderMetadataEntry` — label/value format-neutral;
- `ReaderSession.MetadataEntries` — proiezione da `BookMetadata`/`BookId`;
- `ReaderMetadataFormatter` — wrapping terminal-cell-aware senza Terminal.Gui;
- modalità metadata nel body `ReaderWindow` con offset di scroll effimero.

Comandi:

```text
m                   apre/chiude metadata
↑/↓ oppure j/k       scroll una riga
PgUp/PgDn            scroll una pagina
Esc                  chiude metadata
```

La vista mostra i campi Domain disponibili e non modifica mai la `ReadingLocation`.

## Validation

```bat
.\validate.cmd
```

Gate atteso:

```text
M2.2 VALIDATION PASSED
```

Conteggio statico: **344 Fact + 16 InlineData = 360 casi attesi**.

## ADR/documentazione

- ADR-0038 — La vista metadata proietta solo metadata Domain format-neutral;
- `docs/METADATA_VIEW.md`.

## Prossimo passo dopo il PASS

**M2.3 — Search pre-layout**: ricerca sul testo logico indipendente dal wrapping e salto ai risultati tramite `ReadingLocation`.
