# Roadmap

Per impostazione predefinita ogni milestone si costruisce esclusivamente sulla più recente baseline validata. Se l’utente chiede esplicitamente di proseguire prima del gate, la catena viene marcata come **stacked candidate** e resta non autoritativa fino alla validazione cumulativa.

## M0 — Fondamenta e comprensione EPUB

### M0.1 — Project Foundation — VALIDATED

- .NET 10 / C# 14;
- Central Package Management;
- cinque assembly produttivi;
- test architetturali;
- Terminal.Gui confinato al CLI;
- ADR autoritativi;
- validation gate cross-platform.

### M0.2 — Format-neutral Domain Model — VALIDATED

Validata dall'utente il 07/08/2026.

- `Book` aggregate root;
- metadata, contributor e identificatori;
- reading order primary/supplementary;
- TOC gerarchico;
- content blocks e inline tree;
- `ReadingLocation` section/block/offset UTF-16;
- resource descriptor senza payload;
- invarianti cross-reference;
- Domain privo di semantica EPUB/UI.

### M0.3 — EPUB Container — VALIDATED

Validata dall'utente il 07/08/2026 (Hotfix 1).

- apertura ZIP read-only da path o stream seekable;
- controllo fisico del primo local ZIP header;
- `mimetype` first/stored/no-extra/exact-content;
- indice OCF case-sensitive;
- `META-INF/container.xml` bounded e senza DTD/XXE;
- rootfile multipli e default = primo;
- rootfile media type ed esistenza;
- `OcfPath` con percent-decoding e dot normalization;
- traversal/encoded separator/duplicate-entry rejection;
- nessuna estrazione sul filesystem;
- `EpubContainerException` + `EpubContainerErrorCode`;
- fixture EPUB sintetiche e test negativi.

### M0.4 — OPF Package — VALIDATED

Validata dall'utente il 07/08/2026.

Implementato e validato:

- package namespace/version EPUB 2.0 e EPUB 3.x (`version="3.0"`);
- metadata Dublin Core e unique identifier;
- `dcterms:modified` EPUB 3;
- manifest, media type, properties, fallback e media-overlay;
- risorse locali OCF e URL assoluti remoti;
- spine, `linear`, `toc` legacy e page progression direction;
- relative URL resolution dal Package Document;
- fallback cycle detection e cross-reference validation;
- XML bounded/no-DTD;
- modello intermedio EPUB confinato a `EbookReader.Epub`.

### M0.5 — Navigation — VALIDATED

- EPUB 2 `toc.ncx` tramite `spine/@toc`;
- EPUB 3 `nav.xhtml` tramite manifest `properties=nav`;
- modello intermedio unico;
- TOC annidato;
- EPUB 3 page-list e landmarks;
- path + fragment normalizzati;
- top-level Content Document target validation EPUB 2/3;
- DOCTYPE NCX canonico senza external DTD resolution e con `playOrder` obbligatorio quando presente;
- limiti su bytes, nodi e profondità;
- verifica dell'anchor concreto rinviata a M0.6.

### M0.6 — XHTML to Semantic Domain — VALIDATED

- AngleSharp 1.7.1 confinato a `EbookReader.Epub.Content`;
- `EpubBookReader` produce direttamente il `Book` format-neutral;
- heading, paragraph, quote, pre, list, image e thematic break;
- emphasis/strong, line break e hyperlink;
- normalizzazione whitespace flow;
- `pre` preservato;
- spine `linear` → Primary/Supplementary;
- fallback OPF verso XHTML;
- manifest → `BookResource`;
- anchor XHTML → `ReadingLocation` con offset UTF-16;
- TOC e link interni risolti prima del layout;
- navigation grouping targetless preservato nel Domain;
- nessun CSS completo, JavaScript, browser loader o network retrieval.

### M0.7 — Validation & Diagnostics — VALIDATED

- `EpubPublicationValidator` come facade non-throwing per gli errori EPUB attesi;
- `Valid`, `Invalid` e `Unsupported` distinti;
- diagnostiche machine-readable stabili per Container/Protection/Package/Navigation/Content;
- `META-INF/encryption.xml` ispezionato in modo bounded e senza API crittografiche;
- `rights.xml` registrato come informazione, non come prova automatica di DRM;
- font obfuscation IDPF standard e Adobe legacy distinta dalla cifratura reale;
- cifratura reale fermata prima di Content/AngleSharp e classificata `Unsupported`;
- `CipherReference` root-relative OCF, con traversal/encoded-separator rejection;
- controllo che il font offuscato sia una risorsa font del manifest;
- nessuna decrittazione, derivazione chiavi o circumvention.

## M1 — Primo reader end-to-end

### M1.0 — First Readable EPUB — VALIDATED

```text
ereader book.epub
```

Implementato:

- ingestione tramite `EpubPublicationValidator`;
- output completo del `Book.ReadingOrder` su stdout;
- renderer console deterministico e non paginato;
- heading, paragraph, quote, list, pre, image placeholder e thematic break;
- strong/emphasis/link proiettati come testo logico;
- diagnostiche separate su stderr;
- exit code 0/2/3/4/5 per successo, uso/path, Invalid, Unsupported e I/O;
- EPUB minimo `test-books/m1.0-smoke.epub` eseguito dal validation gate;
- nessun wrapping, viewport, pagina o TUI fullscreen.

### M1.1 — Deterministic Layout Engine — VALIDATED

Implementato in `EbookReader.Layout`:

- `LayoutViewport` con width in celle e height in righe;
- `DeterministicLayoutEngine` su `Book` o `ReadingSection` Domain;
- wrapping Unicode per grapheme con cell width deterministica narrow/wide;
- token lunghi spezzati solo tra grapheme e hard line preservate;
- paragraph spacing e pagine effimere che non iniziano con spacing;
- heading con kind/livello semantico e identità sorgente;
- quote/list indentation bounded e ripetuta sulle continuation line;
- `pre` preservato con tab stop a quattro celle;
- image placeholder e thematic break;
- `BookLayout`, `LayoutPage` e `VisualLine` indipendenti da Terminal.Gui;
- golden test completi 40x10, 80x24, 120x40;
- ADR-0027 e contratto architetturale terminal/format-independent.

Dettagli: [`DETERMINISTIC_LAYOUT.md`](DETERMINISTIC_LAYOUT.md).

### M1.2 — Navigation & Logical Location — VALIDATED

Implementato:

- `VisualLine` conserva range sorgente UTF-16 logici;
- `LayoutLocationResolver` traduce `ReadingLocation` in `LayoutPosition`;
- `LayoutNavigator` implementa riga/pagina precedente-successiva restituendo location logiche;
- `LogicalReadingNavigator` implementa capitolo precedente-successivo e inizio/fine capitolo;
- sezioni `Supplementary` saltate dalla sequenza capitoli primary;
- location a fine blocco, blocchi vuoti e spacing sintetico gestiti deterministicamente;
- reflow su viewport differenti mantiene invariata la location logica;
- `LayoutPosition` resta effimera e non entra in Domain/Application persistence;
- ADR-0028/0029 e `LOGICAL_NAVIGATION.md`.

### M1.3 — Terminal.Gui 2.x Reader TUI — VALIDATED

- `ereader <libro.epub>` apre il reader fullscreen;
- `ereader --plain <libro.epub>` preserva la proiezione lineare M1.0;
- `ReaderSession` testabile conserva solo `ReadingLocation`;
- header con titolo/autore, capitolo e pagina effimera;
- body prodotto dal `BookLayout`;
- footer keyboard-first;
- ↑/↓ o j/k per riga, PgUp/PgDn o h/l/Space per pagina;
- `[ ]` per capitolo, g/G per inizio/fine;
- F1/? help inline, q/Esc uscita;
- Terminal.Gui confinato a `ReaderWindow`/host;
- viewport iniziale di bootstrap;
- ADR-0030/0031 e `READER_TUI.md`.

### M1.4 — Resize Stability — VALIDATED

Implementato:

- `ReaderSession.Reflow(LayoutViewport)`;
- ricostruzione deterministica del `BookLayout` soltanto quando il viewport cambia;
- preservazione esatta della `ReadingLocation`;
- body `Viewport` Terminal.Gui come geometria autoritativa, osservato tramite l’evento pubblico `_body.ViewportChanged`;
- `Console.WindowWidth/Height` usato solo per bootstrap iniziale;
- guard contro reflow ricorsivo e no-op su viewport invariato;
- comportamento stabile con help visibile e terminali estremamente piccoli;
- ADR-0032 e `RESIZE_STABILITY.md`.

## M2 — Esperienza di lettura

### M2.0 — Reading State JSON atomico — VALIDATED

- `state.json` versionato con ultimo libro e `ReadingLocation`;
- scrittura temp + flush-to-disk + rename same-directory;
- restore solo su path + `BookId` + location valida;
- `ereader --resume`;
- `--plain` stateless;
- nessuna pagina/riga/viewport persistita;
- ADR-0033/0034 e `READING_STATE.md`.

### M2.0 Hotfix 1 — Visible Line Scrolling — VALIDATED

- viewport mobile ancorata alla `ReadingLocation`;
- feedback immediato per `↑/↓` e `k/j`;
- PgUp/PgDn invariati;
- ADR-0035.

### M2.0 Hotfix 2 — UI Separators — VALIDATED

- linea orizzontale sotto header;
- linea orizzontale sopra status bar;
- chrome escluso dal `BookLayout`;
- reflow basato sul body viewport aggiornato.

### M2.1 — Interactive TOC — VALIDATED

- overlay gerarchico nel body TUI;
- `t`/`Tab` apre/chiude;
- `↑/↓`, `j/k`, `PgUp/PgDn` navigano l’indice;
- `Enter` salta alla `ReadingLocation`;
- grouping node targetless visibili ma non selezionabili;
- preselezione della voce più vicina alla posizione corrente;
- ADR-0037 e `INTERACTIVE_TOC.md`.

### M2.2 — Metadata View — VALIDATED

- `m` apre/chiude la vista metadata;
- proiezione esclusivamente da `BookMetadata` e `BookId` format-neutral;
- titolo, sottotitolo, contributor/ruoli, lingue, editore, identificatori/schemi, argomenti, diritti e descrizione;
- wrapping Unicode/cell-aware indipendente da Terminal.Gui;
- `↑/↓` o `j/k` scroll di una riga, `PgUp/PgDn` di una pagina;
- `Esc` chiude la vista senza alterare la `ReadingLocation`;
- nessun metadata EPUB-specifico o stato UI persistito;
- ADR-0038 e `METADATA_VIEW.md`.

### M2.3 — Search pre-layout — VALIDATED

- `BookTextSearch` in Application opera su `ContentText.GetPlainText`;
- risultati ordinati come `ReadingLocation` + lunghezza UTF-16;
- query case-insensitive, max 256 code unit UTF-16;
- massimo 10.000 match con flag di truncation;
- `/` apre il prompt inline nella status bar;
- `n` / `N` navigano risultato successivo/precedente con wrap-around;
- primo match selezionato non precedente alla location corrente;
- risultati invarianti rispetto a resize/wrapping;
- ricerca non persistita in `state.json`;
- ADR-0039 e `SEARCH.md`.

### M2.4 — Bookmark logici + Hotfix 2 colori semantici compatibili Terminal.Gui 2.4.17 — CANDIDATE

- `b` toggle bookmark alla `ReadingLocation` corrente;
- `B` elenco bookmark navigabile in TUI;
- `Enter` salto al bookmark selezionato, `d` eliminazione;
- bookmark ordinati in reading order;
- persistenza multi-book in JSON schema 2;
- lettura retrocompatibile dello schema 1;
- nessuna pagina/riga/viewport persistita;
- ADR-0040 e `BOOKMARKS.md`;
- Hotfix 1: style span Strong/Emphasis preservati nel layout, palette TUI e cornici/separatori grigi; ADR-0041 e `READER_COLORS.md`.
- Hotfix 2: applicazione degli schemi via `View.SetScheme(...)`, API effettiva di Terminal.Gui 2.4.17; ADR-0042.

- M2.5 Stable Progress.

## M3 — Library e personalizzazione

- history/local library;
- ricerca library;
- temi;
- keymap configurabile;
- immagini con placeholder/apertura viewer esterno.

## M4 — Navigazione editoriale avanzata

- links/footnotes;
- Unicode typography;
- justification opzionale;
- spread mode.

## Fuori scope

- DRM;
- PDF;
- MOBI/AZW3;
- FB2;
- JavaScript EPUB;
- audio/video EPUB3;
- MathML avanzato;
- browser embedded;
- CSS completo;
- cloud sync/account.
