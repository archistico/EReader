# EReader Domain Model — M0.2

## Scopo

Questo documento descrive il modello interno autoritativo introdotto da M0.2. Il modello rappresenta il contenuto necessario alla lettura senza esporre concetti del formato EPUB.

La regola fondamentale è:

> `EbookReader.Domain` descrive **che cosa è un libro per EReader**, non come quel libro era serializzato nel file sorgente.

## Aggregate root: `Book`

`Book` contiene:

- `BookId Id` — identità interna stabile del libro;
- `BookMetadata Metadata` — metadata editoriali normalizzati;
- `IReadOnlyList<ReadingSection> ReadingOrder` — sequenza logica delle sezioni;
- `TableOfContents TableOfContents` — navigazione gerarchica neutrale;
- `IReadOnlyList<BookResource> Resources` — descriptor delle risorse referenziabili.

Il costruttore verifica la coerenza dell'aggregate completo.

## Identificatori

M0.2 introduce quattro value object distinti:

- `BookId`;
- `SectionId`;
- `BlockId`;
- `ResourceId`.

Non sono stringhe intercambiabili. Questo previene errori in cui, per esempio, un identificatore risorsa viene passato accidentalmente dove serve un identificatore sezione.

Gli adapter futuri sono responsabili di produrre valori deterministici e stabili per una stessa importazione. Il Domain non specifica come tali valori vengano derivati.

## Metadata

`BookMetadata` contiene i metadata che hanno significato indipendentemente da EPUB:

- titolo;
- sottotitolo opzionale;
- lingue;
- contributor con ruolo;
- identificatori editoriali con scheme opzionale;
- descrizione;
- publisher;
- subjects;
- rights.

Non contiene `dc:title`, `opf:*`, refinement EPUB o nodi XML.

### Contributor

`BookContributor` distingue il nome dal ruolo semantico:

- Author;
- Editor;
- Translator;
- Illustrator;
- Narrator;
- Other.

`SortName` è opzionale.

## Reading order

`ReadingOrder` è una sequenza ordinata di `ReadingSection`.

Ogni sezione possiede:

- `SectionId`;
- titolo opzionale;
- `ReadingSectionRole`;
- sequenza lineare di `ContentBlock`.

I ruoli sono:

- `Primary` — parte del normale flusso di lettura;
- `Supplementary` — materiale accessorio raggiungibile ma non necessariamente incluso nella lettura primaria.

Deve esistere almeno una sezione primaria.

## Perché i blocchi sono lineari

EReader non conserva il DOM XHTML come albero Domain. L'adapter trasforma il markup in una sequenza semantica leggibile.

Esempio concettuale:

```html
<blockquote>
  <p>Prima frase.</p>
  <p>Seconda frase.</p>
</blockquote>
```

può diventare:

```text
QuoteBlock(depth=1, "Prima frase.")
QuoteBlock(depth=1, "Seconda frase.")
```

Questo evita che layout, ricerca e navigazione debbano percorrere un DOM arbitrario.

## Content blocks

### `HeadingBlock`

Heading semantico con livello positivo e contenuto inline. L’adapter XHTML userà normalmente 1..6, ma il Domain non impone il limite HTML.

### `ParagraphBlock`

Paragrafo ordinario. Può essere vuoto se il documento sorgente contiene un blocco semantico senza testo.

### `QuoteBlock`

Blocco citazione linearizzato. `Depth` preserva la profondità semantica senza ricostruire un albero di blockquote.

### `ListItemBlock`

Elemento lista linearizzato con:

- `ListKind.Ordered` o `Unordered`;
- profondità;
- ordinale opzionale per liste ordinate.

### `PreformattedBlock`

Testo preformattato preservato senza normalizzazione del whitespace.

### `ImageBlock`

Riferisce una `ResourceId`. Alt text e caption restano proprietà semantiche del punto in cui l'immagine compare.

### `ThematicBreakBlock`

Separatore semantico senza testo logico.

## Inline content

Il contenuto inline conserva solo la semantica utile al reader:

- `TextRun`;
- `EmphasisSpan`;
- `StrongSpan`;
- `HyperlinkSpan`;
- `LineBreakInline`.

Gli span possono essere annidati. `ContentText.GetPlainText` produce una proiezione deterministica indipendente dal layout.

Esempio:

```text
Text("Hello ")
Strong(
  Text("bold")
  LineBreak
)
Emphasis(Text("world"))
```

produce:

```text
Hello bold\nworld
```

Questa proiezione diventerà la base per ricerca, progress e risoluzione degli offset.

## Link

`HyperlinkSpan` utilizza un `LinkTarget` neutrale:

- `InternalLinkTarget` → `ReadingLocation`;
- `ExternalLinkTarget` → `System.Uri` assoluto.

Il Domain non contiene href relativi, fragment XHTML o path EPUB. Questi vengono risolti dall'adapter prima di costruire il modello finale.

## ReadingLocation

La posizione logica è composta da:

```text
SectionId
BlockId?
CharacterOffset
```

### Posizione di sezione

`BlockId == null` significa inizio sezione e richiede `CharacterOffset == 0`.

### Posizione di blocco

Con `BlockId` presente, l'offset è relativo alla stringa prodotta da `ContentText.GetPlainText(block)`.

### Unità dell'offset

L'offset usa indici UTF-16 di `System.String`, cioè la stessa unità nativa usata dalle API .NET di ricerca e slicing.

Per esempio:

```text
"A😀B".Length == 4
```

Questo **non** significa quattro caratteri visuali e non equivale a quattro celle terminale. Il layout dovrà tradurre separatamente Unicode/grapheme/cell width.

## TOC

`TableOfContents` contiene una lista di `NavigationItem`. Ogni item contiene:

- label;
- `ReadingLocation` target;
- children.

La gerarchia è conservata, ma il target è già risolto sul modello logico.

## Resources

`BookResource` è un descriptor:

- `ResourceId`;
- `ResourceKind`;
- media type;
- nome opzionale;
- byte length opzionale.

M0.2 **non mette il payload binario nel Domain**. Il Domain non possiede stream, byte array, path ZIP o handle filesystem.

`ImageBlock` può riferire solo una risorsa esistente con `ResourceKind.Image`.

## Invarianti dell'aggregate

Alla costruzione `Book` verifica:

1. reading order non vuoto;
2. almeno una sezione `Primary`;
3. `SectionId` univoci nel libro;
4. `BlockId` univoci dentro ogni sezione;
5. `ResourceId` univoci;
6. risorse immagine risolvibili e tipizzate correttamente;
7. target TOC risolvibili;
8. target dei link interni risolvibili;
9. offset logici non oltre la lunghezza del blocco.

Un `Book` costruito con successo è quindi internamente coerente rispetto ai riferimenti previsti da M0.2.

## Snapshot e immutabilità pratica

Le collection in ingresso vengono copiate e pubblicate come read-only snapshot. Il chiamante non può modificare il `Book` modificando successivamente la `List<T>` usata nel costruttore.

Gli oggetti Domain non espongono setter pubblici.

## Cosa NON appartiene al Domain

- ZIP entry;
- `container.xml`;
- OPF package;
- manifest EPUB;
- spine/itemref EPUB;
- NCX;
- `nav.xhtml`;
- AngleSharp DOM;
- CSS;
- Terminal.Gui View;
- numero pagina;
- viewport;
- coordinate terminale;
- path del file sorgente;
- payload delle risorse.

## Boundary con M0.3–M0.6

Le milestone EPUB possono avere DTO e parser interni specifici del formato. Solo quando le informazioni sono sufficienti devono creare il `Book` Domain finale.

Da M0.6 fragment e href vengono risolti nell’adapter EPUB dopo la costruzione dei blocchi semantici: il Domain riceve soltanto `ReadingLocation` già risolte, con offset UTF-16.


## Evoluzione M0.6 — navigation grouping

`NavigationItem.Target` è nullable per rappresentare gruppi editoriali non direttamente navigabili. Un nodo targetless deve avere almeno un figlio. La scelta è format-neutral e coperta da ADR-0022.
