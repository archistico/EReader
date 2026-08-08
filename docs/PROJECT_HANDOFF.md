# Project Handoff — EReader M3.5 Hotfix 1 — XHTML Smoke Fixture Alignment

## Baseline

- **Baseline autoritativa validata:** `EReader_M3.4_Hotfix1_HelpContract_NET10_Candidate.zip`.
- Gate utente: `M3.4 HOTFIX 1 VALIDATION PASSED`.
- **Candidate corrente:** `EReader_M3.5_Hotfix1_XhtmlSmokeFixture_NET10_Candidate.zip` — M3.5 Hotfix 1, costruita esclusivamente sopra la candidate M3.5 derivata da M3.4 Hotfix 1.
- Target: .NET 10 / C# 14 / Terminal.Gui 2.4.17 / AngleSharp 1.7.1.

## M3.5

- `EbookReader.Application.Links.BookHyperlinkIndex` indicizza gli `HyperlinkSpan` Domain in range UTF-16 logici.
- `ReaderSession.CurrentHyperlink` preferisce il link che contiene l'esatta ReadingLocation; in fallback usa il primo link che interseca la VisualLine corrente.
- `Enter` segue un link interno o delega un link esterno al sistema operativo; senza link continua a gestire l'immagine M3.4.
- I link interni pushano l'origine in uno stack runtime bounded a 128 elementi.
- `Backspace` torna all'origine più recente.
- `SystemExternalLinkService` ammette soltanto http/https/mailto e usa `UseShellExecute=true` dopo azione esplicita.
- Nessun HttpClient, browser embedded o fetch di rete viene introdotto.

## M3.5 Hotfix 1

- Corretto soltanto `test-books/m3.5-link-smoke.epub`.
- Rimossi i `<!DOCTYPE html>` da `EPUB/nav.xhtml` e `EPUB/Text/ch1.xhtml`.
- Entrambi i documenti sono ora XHTML/XML UTF-8 espliciti e compatibili con il parser sicuro (`DtdProcessing.Prohibit`).
- Il package EPUB mantiene `mimetype` come prima entry, non compressa e senza extra field.
- Nessun file sotto `src/` cambia rispetto alla candidate M3.5 originale.

## Invarianti

- `ReadingLocation` resta l'unica coordinata durevole.
- Resize/reflow non cambia l'identità/range logico del link.
- `state.json` resta schema 3.
- `config.json` resta schema 1.
- Link stack e current link non vengono persistiti.
- Domain non viene modificato.
- M3.4 image preview resta fallback di Enter quando non c'è un link azionabile.

## Gate

Da estrazione pulita:

```bat
.\validate.cmd
```

Esito atteso:

```text
M3.5 HOTFIX 1 VALIDATION PASSED
```

Conteggio statico previsto: **454 casi** (438 Fact + 16 InlineData; 4 Theory).
