# Project Handoff — EReader M3.6 — Footnotes / Endnotes UX

## Baseline

- Baseline autoritativa validata: `EReader_M3.5_Hotfix1_XhtmlSmokeFixture_NET10_Candidate.zip`.
- Gate baseline: `M3.5 HOTFIX 1 VALIDATION PASSED`.
- Candidate corrente: `EReader_M3.6_FootnotesEndnotesUX_NET10_Candidate.zip`.

## M3.6

- Domain: nuovo `HyperlinkRole.Generic/NoteReference`; `HyperlinkSpan` conserva il ruolo con default `Generic`.
- EPUB: `epub:type="noteref"` viene riconosciuto come token whitespace-separated e tradotto a `NoteReference`; nessuna stringa EPUB passa in Application.
- Application: `BookHyperlink` / `BookHyperlinkIndex` conservano il ruolo insieme ai range logici UTF-16.
- TUI: `NOTA` nell'header, `Enter nota` nel footer, messaggio di ritorno specifico; la navigazione usa `ReadingLocation` e lo stack Backspace M3.5.
- Persistenza: invariata (`state.json` schema 3, `config.json` schema 1).
- Fixture: `test-books/m3.6-notes-smoke.epub`.
- ADR: 0050.

## Gate atteso

```text
M3.6 VALIDATION PASSED
```

M3.6 resta CANDIDATE fino al gate locale dell'utente.


Audit statico M3.6: **444 Fact + 4 Theory + 16 InlineData = 460 casi attesi**.


## M3.6 Hotfix 1

- Corregge esclusivamente il contratto del test help M3.6; `src/` resta byte-identical alla candidate M3.6.
- Gate atteso: `M3.6 HOTFIX 1 VALIDATION PASSED`.
- Diagnostica temi: `semantic-dark` = testo bianco, heading cyan, strong verde, emphasis giallo; `monochrome` = bianco/grigio con Bold/Italic. Il tema è persistito in `config.json`.
