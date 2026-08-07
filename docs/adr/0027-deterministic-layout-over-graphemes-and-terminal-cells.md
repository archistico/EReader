# ADR-0027 — Layout deterministico su grapheme e celle terminale

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

Il Domain usa offset logici UTF-16 per ricerca, anchor e persistenza, ma una code unit non equivale a un carattere visuale né a una cella terminale. Il wrapping non può dipendere da Terminal.Gui o dalla console corrente e non deve modificare le identità Domain.

M1.1 deve inoltre produrre risultati ripetibili per viewport uguali, così resize, navigazione e TUI future possono appoggiarsi a un boundary verificabile.

## Decisione

Il layout vive esclusivamente in `EbookReader.Layout` e dipende solo dal Domain.

- `LayoutViewport` esplicita larghezza in celle e altezza in righe;
- `StringInfo` identifica i grapheme che non possono essere spezzati;
- una tabella deterministica di cell width distingue combining/control, rune narrow e gamme wide/emoji;
- `DeterministicLayoutEngine` converte blocchi semantici in `VisualLine` e poi in `LayoutPage`;
- ogni riga conserva `SectionId`, `BlockId` e kind semantico, ma M1.1 non promette ancora la mappatura completa degli offset logici;
- i numeri pagina sono effimeri e dipendono dal viewport;
- `pre` conserva whitespace con tab stop fisso a quattro celle;
- tre golden snapshot autoritative coprono 40×10, 80×24 e 120×40.

## Conseguenze

- il risultato è indipendente da OS, dimensione della console e framework UI;
- un grapheme non viene mai separato, mentre token lunghi possono andare a capo tra grapheme;
- layout e Domain mantengono sistemi di coordinate distinti;
- M1.2 potrà aggiungere `ReadingLocation → viewport` senza spostare numeri pagina nel Domain;
- la tabella cell-width è intenzionalmente deterministica e non interroga capability terminale variabili.

## Alternative considerate

### Usare `string.Length`

Respinto perché misura code unit UTF-16 e spezzerebbe surrogate pair o sequenze combining.

### Delegare il wrapping a Terminal.Gui

Respinto perché accoppierebbe il layout all'outer adapter e renderebbe più fragili golden test e resize.

### Persistire il numero pagina

Respinto perché una pagina cambia con il viewport e viola ADR-0007/0011.

### Implementare l'intera Unicode UAX #11 dinamicamente

Rinviato: per M1.1 serve una classificazione locale, bounded e stabile. L'approssimazione adottata copre combining mark, CJK, Hangul, emoji e principali gamme wide senza dipendenze runtime esterne.
