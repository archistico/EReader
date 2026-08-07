# M1.2 — Navigation & Logical Location

## Obiettivo

M1.2 collega il sistema di coordinate logico introdotto nel Domain con il layout deterministico M1.1, senza trasformare pagine e righe in stato persistibile.

## Due sistemi di coordinate

### Coordinate stabili

```text
ReadingLocation
├── SectionId
├── BlockId?
└── CharacterOffset UTF-16
```

Questa è la coordinata autoritativa per lettura, anchor, bookmark, ricerca e futuro stato JSON.

### Coordinate effimere

```text
LayoutPosition
├── PageNumber   1-based
└── LineIndex    0-based nella pagina
```

Una `LayoutPosition` vale soltanto per uno specifico `BookLayout` e uno specifico `LayoutViewport`.

## Mapping location → viewport

`DeterministicLayoutEngine` associa alle righe di contenuto un intervallo sorgente:

```text
VisualLine
├── SectionId
├── BlockId
├── SourceStartOffset   inclusive
└── SourceEndOffset     exclusive
```

Gli offset usano le stesse code unit UTF-16 di `ReadingLocation`.

`LayoutLocationResolver.Locate(book, layout, location)` restituisce quindi la pagina/riga che contiene la location nel viewport corrente.

Una location all'esatto end-of-block viene associata all'ultima riga del blocco. Blocchi vuoti senza riga visuale vengono localizzati deterministicamente sul primo blocco leggibile successivo, oppure sul precedente se non esiste un successivo.

## Reflow

La stessa location può produrre coordinate diverse:

```text
ReadingLocation(section, block, 123)
        │
        ├── viewport 40×10  → pagina 4, riga 2
        └── viewport 120×40 → pagina 1, riga 15
```

La `ReadingLocation` non cambia.

## Navigazione visuale

`LayoutNavigator` espone:

- `NextLine`;
- `PreviousLine`;
- `NextPage`;
- `PreviousPage`.

I metodi ricevono una `ReadingLocation` e restituiscono una nuova `ReadingLocation?`. Le righe sintetiche di spacing non diventano posizioni di lettura.

La navigazione pagina porta alla prima riga logica della pagina adiacente.

## Navigazione logica di capitolo

`LogicalReadingNavigator` vive in `EbookReader.Application` e non conosce il layout:

- `ChapterStart`;
- `ChapterEnd`;
- `NextChapter`;
- `PreviousChapter`.

Per precedente/successivo vengono considerate le sezioni `Primary`; le sezioni `Supplementary` vengono saltate.

## Preformatted e Unicode

Il mapping mantiene gli offset logici anche quando:

- un emoji occupa due code unit UTF-16 ma due celle terminale;
- un grapheme contiene più rune;
- un tab di `<pre>` viene espanso in più celle;
- una hard line viene spezzata in più visual line.

## Persistenza

M1.2 non introduce persistenza.

Quando M2.0 salverà lo stato, il contratto consentito rimarrà `ReadingLocation`; `LayoutPosition`, `LayoutPage.Number` e `LineIndex` non devono essere serializzati come posizione di lettura.

## Boundary verso M1.3/M1.4

M1.3 potrà usare i navigator dalla TUI Terminal.Gui senza reimplementare la logica nei View.

M1.4 potrà implementare il resize come:

```text
ReadingLocation corrente
      ↓
nuovo LayoutViewport
      ↓
nuovo BookLayout
      ↓
LayoutLocationResolver.Locate(...)
```
