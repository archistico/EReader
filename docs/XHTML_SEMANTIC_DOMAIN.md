# M0.6 — XHTML to Semantic Domain

Stato: **VALIDATED**.  
Ultima baseline autoritativa validata: **M2.0 Hotfix 2 — UI Separators**.

## Obiettivo

M0.6 introduce il confine che trasforma i Content Document XHTML EPUB in oggetti del modello interno format-neutral. L'output pubblico è `EbookReader.Domain.Books.Book`; nessun tipo AngleSharp, HTML, XHTML, OPF, OCF o EPUB attraversa il boundary verso il Domain.

```text
EpubContainer
    ↓
EpubPackageReader
    ↓
EpubNavigationReader
    ↓
EpubBookReader
    ├── AngleSharp HtmlParser
    ├── XHTML → semantic drafts
    ├── anchor registry
    ├── link/TOC resolution
    └── metadata/resource mapping
            ↓
           Book
```

## Dipendenza AngleSharp

M0.6 attiva AngleSharp esclusivamente nel progetto `EbookReader.Epub`. La versione centrale è `1.7.1`, stabile e con target `net10.0` nativo. Non vengono attivati loader HTTP, JavaScript, CSSOM o browsing context: il parser riceve soltanto una stringa già letta dal container OCF.

## Reading order

Ogni `spine/itemref` produce una `ReadingSection`:

- `linear=yes` o omesso → `ReadingSectionRole.Primary`;
- `linear=no` → `ReadingSectionRole.Supplementary`;
- se la risorsa spine non è XHTML, viene seguita la fallback chain OPF fino a `application/xhtml+xml`;
- `SectionId` è deterministico e include posizione nello spine + `idref`.

## Blocchi semantici

Mapping M0.6:

| XHTML | Domain |
|---|---|
| `h1`…`h6` | `HeadingBlock` |
| `p` e flow text | `ParagraphBlock` |
| `blockquote` | `QuoteBlock` |
| `pre` | `PreformattedBlock` |
| `ol/ul > li` | `ListItemBlock` |
| `img` / `figure img` | `ImageBlock` |
| `hr` | `ThematicBreakBlock` |

Container strutturali come `section`, `article`, `main`, `div`, `aside`, `header` e `footer` vengono attraversati senza diventare tipi Domain dedicati.

`script`, `style`, `template`, `nav` e `form` non vengono proiettati nel reading text.

## Inline tree

Mapping M0.6:

| XHTML | Domain |
|---|---|
| text node | `TextRun` |
| `em`, `i` | `EmphasisSpan` |
| `strong`, `b` | `StrongSpan` |
| `a` interno al reading order | `HyperlinkSpan` + `InternalLinkTarget` |
| `a epub:type="noteref"` interno | `HyperlinkSpan` + `InternalLinkTarget` + `HyperlinkRole.NoteReference` |
| `a` `http:`, `https:` o `mailto:` | `HyperlinkSpan` + `ExternalLinkTarget` |
| `br` | `LineBreakInline` |
| altro inline | contenuto attraversato e preservato |

Un link locale verso una risorsa che non appartiene al reading order conserva il testo ma non diventa un hyperlink Domain. È una scelta conservativa: il Domain M0.6 modella destinazioni di lettura, non un generico browser di risorse.

Gli URL `file:`, `javascript:`, `data:` e gli altri schemi assoluti non esplicitamente ammessi non diventano link attivabili; il testo rimane leggibile.

## Whitespace

Il flow text viene normalizzato in modo deterministico:

- sequenze di whitespace collassate a un singolo spazio;
- whitespace iniziale/finale non produce testo spurio;
- `br` produce `\n` logico;
- `pre` conserva il testo esattamente come ricevuto dal DOM.

Questa normalizzazione avviene prima del layout e quindi resta stabile rispetto a larghezza/altezza del terminale.

## Anchor e ReadingLocation

Gli `id` XHTML vengono risolti durante la conversione semantica.

```text
Text/ch1.xhtml#middle
        ↓
(path, fragment)
        ↓
SectionId + BlockId + UTF-16 CharacterOffset
        ↓
ReadingLocation
```

Un anchor su un block element punta all'inizio del blocco. Un anchor inline punta all'offset logico esatto prima del contenuto dell'elemento. L'offset usa unità UTF-16 .NET, coerenti con ADR-0011 e `string.Length`.

Per esempio:

```html
<p>Alpha <span id="middle">beta</span></p>
```

produce un anchor con offset `6` (`"Alpha "`). Un emoji astrale conta due code unit UTF-16.

Gli anchor duplicati nello stesso Content Document sono rifiutati. TOC e link interni verso fragment inesistenti sono rifiutati in M0.6, non rinviati al layout.

## Navigation grouping

EPUB 3 consente nodi TOC di raggruppamento basati su `span`, senza `href`. Il Domain ora consente quindi:

```text
NavigationItem
├── Label
├── Target?        nullable
└── Children[]
```

`Target = null` è valido solo quando esiste almeno un figlio. È una capacità format-neutral e non una dipendenza da EPUB.

## Risorse

Ogni risorsa locale del manifest viene descritta con `BookResource`. Il kind è derivato dal media type:

- `image/*` → Image;
- `text/css` → Stylesheet;
- media type font → Font;
- `audio/*` → Audio;
- `video/*` → Video;
- altro → Other.

Il Domain continua a non contenere payload binari. `ImageBlock` punta al `ResourceId` derivato dall'id del manifest.

## Limiti

Per ogni Content Document:

- massimo 8 MiB non compressi letti dal relativo ZIP entry stream;
- massimo 250.000 nodi DOM attraversati;
- profondità massima 64;
- massimo 50.000 blocchi semantici.

Nessun network retrieval, nessuna estrazione su filesystem e nessuna esecuzione di script.

## Cosa M0.6 non fa

- CSS completo o layout CSS;
- JavaScript;
- SVG/MathML semantici avanzati;
- media overlays;
- fixed-layout EPUB;
- rendering immagini nel terminale;
- pagination/word wrapping;
- TUI di lettura.

Questi confini mantengono M0.6 concentrata sulla trasformazione del contenuto sorgente in un modello logico stabile.


Da M3.6 il token EPUB `noteref` viene consumato esclusivamente in questo adapter e non attraversa il boundary come stringa EPUB.
