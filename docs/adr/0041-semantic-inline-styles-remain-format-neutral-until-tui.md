# ADR-0041 — Gli stili inline restano semantici fino al boundary TUI

- **Stato:** Accepted
- **Data:** 2026-08-08

## Contesto

Il Domain conserva già `StrongSpan` ed `EmphasisSpan`, ma fino a M2.4 il layout li appiattiva in testo semplice. La richiesta di distinguere visivamente titoli, grassetti e corsivi non deve introdurre colori Terminal.Gui nel Domain o nel layout, né ricostruire a posteriori la semantica dal testo renderizzato.

## Decisione

- `EbookReader.Layout` conserva sulle `VisualLine` solo hint semantici format-neutral tramite `VisualTextStyle` (`Strong`, `Emphasis`) e `VisualTextSpan`.
- Gli style span usano indici UTF-16 locali alla `VisualLine.Text` e vengono mantenuti durante wrapping, prefissi di quote/liste e grapheme Unicode.
- `VisualLineKind.Heading` identifica i titoli e resta separato dagli style span inline.
- Nessun colore, `Scheme`, `Attribute` o tipo Terminal.Gui entra in Domain/Application/Layout.
- Il boundary `EbookReader.Cli` traduce la semantica nella palette M2.4 Hotfix 1:
  - heading → cyan/azzurro;
  - strong → verde + `TextStyle.Bold`;
  - emphasis → giallo + `TextStyle.Italic`;
  - strong+emphasis → verde + bold+italic;
  - testo ordinario → bianco;
  - frame/separatori → grigio;
  - background → nero.
- I terminali che non rendono bold/italic mantengono comunque la distinzione cromatica.

## Conseguenze

- La semantica editoriale sopravvive al layout senza legare il motore a una specifica TUI.
- Colori e tema possono cambiare in futuro senza modificare EPUB parser, Domain o algoritmo di wrapping.
- Heading e inline style possono essere renderizzati diversamente anche da futuri frontend.
- La `ReadingLocation`, gli offset sorgente, la ricerca e la paginazione restano indipendenti dalla palette.

## Alternative considerate

### Colorare l'intero paragrafo se contiene bold/italic

Rifiutato: perde la granularità inline e produce un risultato semanticamente falso.

### Ricostruire bold/italic dalla sorgente EPUB nella View

Rifiutato: reintrodurrebbe EPUB nel boundary TUI e duplicazione di parsing.

### Memorizzare direttamente i colori nel `BookLayout`

Rifiutato: le scelte cromatiche appartengono al frontend Terminal.Gui, non al layout format-neutral.
