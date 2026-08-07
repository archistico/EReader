# M1.1 — Deterministic Layout Engine

## Obiettivo

M1.1 introduce un layout terminal-independent nel progetto `EbookReader.Layout`. Il motore riceve soltanto il `Book` o una `ReadingSection` format-neutral e produce righe visuali e pagine effimere per un viewport esplicito.

```text
Book / ReadingSection
        ↓
LayoutViewport(width, height)
        ↓
DeterministicLayoutEngine
        ↓
BookLayout → LayoutPage[] → VisualLine[]
```

Nessun tipo Terminal.Gui, EPUB, AngleSharp o accesso a `Console.WindowWidth/Height` entra nel motore.

## Contratto pubblico

- `LayoutViewport`: larghezza in celle terminale (minimo 2) e altezza in righe (minimo 1);
- `DeterministicLayoutEngine.Layout(Book, viewport)` per l'intero reading order;
- `DeterministicLayoutEngine.Layout(ReadingSection, viewport)` per una sezione;
- `BookLayout`: risultato immutabile associato al viewport;
- `LayoutPage`: pagina visuale 1-based, effimera e mai usata come posizione persistente;
- `VisualLine`: testo, larghezza in celle, kind semantico e `SectionId`/`BlockId` sorgente.

La traduzione completa `ReadingLocation → viewport` resta M1.2.

## Unicode e wrapping

Il wrapping opera sui text element di `StringInfo`, quindi non spezza una sequenza grapheme. `TerminalCellWidth` assegna:

- zero celle a control, format e combining mark;
- due celle alle principali gamme CJK, Hangul, emoji e simboli wide;
- una cella agli altri rune.

La larghezza di un grapheme è il massimo dei rune che lo compongono. Le sequenze di whitespace flow diventano un separatore; i token più larghi della riga vengono spezzati solo tra grapheme. Le interruzioni logiche `\n` restano hard break.

## Blocchi

| Domain block | Layout M1.1 |
|---|---|
| `HeadingBlock` | `VisualLineKind.Heading` + livello semantico |
| `ParagraphBlock` | flow wrapping + riga di spaziatura |
| `QuoteBlock` | prefisso `> ` per profondità su ogni riga |
| `ListItemBlock` | marker/ordinale e continuation indent |
| `PreformattedBlock` | whitespace preservato, tab stop deterministico a 4 celle |
| `ImageBlock` | placeholder testuale con alt/caption |
| `ThematicBreakBlock` | `---` |

Quote e liste limitano l'indentazione visibile al viewport, evitando allocazioni proporzionali a profondità Domain non limitate.

## Paginazione

L'altezza divide la sequenza di righe visuali in pagine. Una pagina:

- contiene al massimo `viewport.Height` righe;
- non inizia con una riga di sola spaziatura;
- mantiene ordine e identità sorgente;
- viene ricalcolata a ogni resize.

Il numero pagina non appartiene al Domain e non deve essere persistito.

## Golden test

La stessa sezione campione è verificata integralmente sui viewport:

- 40×10;
- 80×24;
- 120×40.

I golden test coprono wrapping, pagine, heading, quote, liste, preformatted, immagini, thematic break e spaziatura.


## Estensione M1.2

M1.2 estende `VisualLine` con `SourceStartOffset`/`SourceEndOffset` UTF-16. Il testo e la paginazione golden M1.1 non cambiano; il motore conserva ora anche il mapping necessario a `ReadingLocation → viewport`. Vedere [`LOGICAL_NAVIGATION.md`](LOGICAL_NAVIGATION.md) e ADR-0028.
