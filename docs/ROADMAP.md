# Roadmap

**Baseline autoritativa corrente:** M3.8 Hotfix 1 VALIDATED (`M3.8 HOTFIX 1 VALIDATION PASSED`, 08/08/2026).  
**Candidate corrente:** M3.9 — Defensive EPUB Loading & Input Security.  
**Priorità immediata:** validare M3.9, poi M3.10–M3.13 reliability/recovery prima di M4.0.

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

### M2.4 — Bookmark logici + colori semantici — VALIDATED

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

### M2.5 — Stable Progress — VALIDATED

- `BookProgressIndex` precomputa il peso logico del `Book.ReadingOrder`;
- unità = code unit UTF-16 di `ContentText.GetPlainText(block)`;
- `ReadingLocation.CharacterOffset` è usato direttamente nello stesso spazio di coordinate;
- percentuale indipendente da pagina, wrapping, viewport e resize;
- sezioni supplementary incluse perché fanno parte del reading order format-neutral;
- nessuna percentuale persistita in `state.json`;
- header TUI mostra pagina effimera e percentuale stabile come informazioni distinte;
- ADR-0043 e `STABLE_PROGRESS.md`.

## M3 — Library, personalizzazione e reliability

### M3.0 — Library & Reading History — VALIDATED

- cronologia bounded degli ultimi 200 EPUB realmente aperti;
- stato JSON schema 3 con path, BookId, titolo/autore, ultimo accesso e ReadingLocation;
- `ereader --library` apre la selezione fullscreen;
- `ereader --history` stampa la cronologia su stdout;
- apertura diretta di un libro recente ripristina la sua posizione logica;
- nessun database, scan automatico di directory o percentuale persistita;
- schema 1/2 retrocompatibili e promossi dal lastBook;
- ADR-0044 e `LOCAL_LIBRARY.md`.

### M3.1 — Library Search — VALIDATED

- `/` apre il filtro live nella libreria;
- matching case/accent-insensitive su titolo, autore, nome file e path;
- fallback fuzzy per sottosequenza con ranking deterministico;
- `Enter` applica, `Esc` annulla durante l'input o cancella un filtro attivo;
- query transiente, non persistita;
- ADR-0045 e `LIBRARY_SEARCH.md`.

### M3.2 — Themes — VALIDATED

- `c` cicla Semantico scuro / Carta chiara / Monocromatico;
- `ReaderTheme` mappa i ruoli semantici del Layout a attributi Terminal.Gui;
- tema predefinito = palette M2.4 validata;
- header/footer/separatori/body cambiano insieme;
- nessun tema o colore entra in Domain/Application/Layout;
- scelta tema transiente, non persistita nello state JSON;
- ADR-0046 e `THEMES.md`.

### M3.3 — Configurable Keymap & Preferences — VALIDATED

- validata insieme a M3.2 tramite il gate cumulativo `M3.2+M3.3 HOTFIX 1 STACKED VALIDATION PASSED`;
- `config.json` schema 1 separato da `state.json` schema 3;
- `ereader --config-path` e `ereader --init-config`;
- override percorso tramite `EREADER_CONFIG_FILE`;
- tema selezionato persistito con id stabile;
- alias stampabili del reader configurabili e case-sensitive;
- singolo grapheme per binding, collisioni rifiutate;
- binding mancanti ereditano i default;
- frecce/PgUp/PgDn/Space/Tab/Enter/Backspace/Esc/F1 restano tasti speciali fissi;
- configurazione bounded 64 KiB e scritta atomicamente;
- ADR-0047 e `CONFIGURATION_KEYMAP.md`.

### M3.4 — Images — VALIDATED

- il placeholder testuale Domain/Layout con alt/caption resta il fallback universale;
- `ReaderSession.CurrentImage` identifica l'`ImageBlock` alla `ReadingLocation` corrente senza caricare payload;
- header mostra il media type dell'immagine corrente e footer propone `Enter immagine`;
- `Enter` nella lettura normale apre esplicitamente il raster locale nel viewer associato dal sistema operativo;
- `EpubImageResourceReader` risolve la risorsa dal manifest tramite `ResourceId`, non tramite path inventati dal Domain;
- supporto preview: JPEG, PNG, GIF, WebP locali; SVG e risorse remote restano placeholder;
- payload bounded a 16 MiB e letto in memoria soltanto su richiesta;
- il CLI crea un file temporaneo con estensione derivata dal media type e tenta cleanup alla chiusura della TUI;
- nessun browser embedded, network retrieval, payload persistito o modifica a `state.json`/`config.json`;
- ADR-0048 e `IMAGES.md`.

### M3.5 — Interactive Hyperlinks & Back Stack — VALIDATED

- indice hyperlink pre-layout su range logici UTF-16;
- `Enter` segue il link esatto o il primo link che interseca la riga corrente;
- link interno → `ReadingLocation` Domain già risolta;
- stack Backspace transiente bounded a 128 origini;
- external link `http`/`https`/`mailto` delegato esplicitamente al sistema operativo;
- nessun network fetch, browser embedded o persistenza dello stack;
- `Enter` immagine M3.4 resta fallback quando non è disponibile un link;
- ADR-0049 e `HYPERLINKS.md`.
- Hotfix 1: smoke EPUB riallineato a XHTML/XML senza DOCTYPE; nessuna modifica produttiva.

### M3.6 — Footnotes / Endnotes UX — VALIDATED

- `epub:type="noteref"` mappato al ruolo Domain format-neutral `HyperlinkRole.NoteReference`;
- header/footer `NOTA` / `Enter nota`;
- salto alla nota tramite la stessa `ReadingLocation` M3.5;
- ritorno immediato con Backspace sullo stack bounded M3.5;
- note non marcate continuano a funzionare come hyperlink interni generici;
- nessuna coordinata di layout, modalità nota o stack persistito;
- ADR-0050 e `FOOTNOTES_ENDNOTES.md`.

### M3.7 — Highlights & Personal Notes — VALIDATED

M3.7 Hotfix 1 è stata validata dall'utente l'08/08/2026 con gate `M3.7 HOTFIX 1 VALIDATION PASSED`; è stata poi superata come baseline autoritativa da M3.8 Hotfix 1.

- F2 highlight della riga logica corrente;
- F3 nota personale alla ReadingLocation corrente;
- F4 elenco annotazioni con navigazione/eliminazione;
- range UTF-16 same-block e note book-scoped;
- state schema 4 retrocompatibile con 1/2/3;
- config schema 1 invariato;
- rendering highlight line-level confinato al CLI/TUI;
- ADR-0051 e `HIGHLIGHTS_NOTES.md`;
- Hotfix 1: ricollocazione del test architetturale M3.7, helper `TemporaryDirectory` nei test annotazioni e import Domain necessario a `BlockId`; nessuna variazione del contratto funzionale M3.7.

## M3.8–M3.13 — Reliability, diagnostics & input safety

Questa fase ha priorità rispetto alla libreria gestita M4.0. Il principio guida è:

> **Un EPUB può essere illeggibile. EReader no.**

Un errore recuperabile deve degradare soltanto la risorsa o parte del libro interessata. Un errore irreversibile del documento può impedire l'apertura di quel libro, ma non deve corrompere lo stato né rendere inutilizzabile l'applicazione.

### M3.8 — Diagnostics Foundation & Failure Taxonomy — HOTFIX 1 VALIDATED

Obiettivo: estendere il contratto M0.7 dall'ingestione all'intero reader senza romperlo.

- preservare `Valid` / `Invalid` / `Unsupported` come esito della facade EPUB;
- introdurre `Information`, `Warning`, `RecoverableError`, `FatalDocumentError`, `InternalError` nell’Application layer;
- codice diagnostico stabile e machine-readable;
- messaggio umano separato dai dettagli tecnici;
- origine/componente, path OCF/risorsa/target quando applicabili;
- recovery dichiarata esplicitamente;
- esiti applicativi `Success`, `SuccessWithDiagnostics`, `DocumentUnreadable`, `InternalFailure`;
- nessuna pagina/riga/viewport nella diagnostica persistente;
- bridge EPUB → tassonomia reader-wide confinato al CLI/composition root;
- nessun catch-all che trasformi bug EReader in EPUB non valido;
- output CLI esplicito `DOCUMENT_UNREADABLE` per `Invalid`/`Unsupported`.

Documenti guida: `DIAGNOSTICS.md` e `EPUB_FAILURE_MODEL.md`.

### M3.9 — Defensive EPUB Loading & Input Security — CANDIDATE

Audit e hardening dell'EPUB come input non attendibile, senza recovery generale.

Implementato nella candidate:

- limiti Container: 100.000 entry archivio, 256 MiB decompressi per entry e 2 GiB cumulativi dichiarati;
- compression-ratio guard: oltre `500:1` per entry da almeno 16 MiB decompressi;
- rifiuto delle entry ZIP Unix di tipo speciale, inclusi symlink, e conferma del modello no-extraction;
- stream ZIP validato che traduce corruzione/metodo non supportato e controlla la lunghezza dichiarata a EOF;
- `EpubPublicationValidator` classifica come Container anche corruzioni ZIP emerse dopo l'apertura iniziale;
- rifiuto di prefissi drive/schema, anche percent-encoded, nei path OCF ZIP/reference oltre ai traversal e separator encoded già validati;
- manifest remoto ristretto a `http`/`https`; nessun retrieval viene introdotto;
- fallback OPF bounded a 64 passaggi e cycle detection preservato;
- decoding XHTML UTF-8/UTF-16 strict e rifiuto dei control character XML non ammessi;
- limiti XML già esistenti su container/OPF/navigation/protection preservati;
- nessun catch-all di eccezioni interne EReader e nessun repair del documento.

Documento guida: `EPUB_SECURITY_MODEL.md`. ADR: `0053-defensive-epub-input-stays-virtual-and-bounded.md`.

### M3.10 — EPUB Recovery & Degraded Reading — PLANNED

Definire una matrice problema → recovery/esito verificabile.

- cover/CSS/metadata opzionali mancanti;
- TOC assente con spine ancora leggibile;
- immagini mancanti/corrotte → placeholder;
- risorse remote → nessun fetch;
- Content Document o spine item problematici: recovery solo quando non ambigua;
- `container.xml`, OPF o reading order non determinabili → documento irrecuperabile;
- nessun guessing silenzioso;
- apertura con commit tardivo: lo stato valido precedente resta intatto fino a successo.

Documento guida: `EPUB_RECOVERY_POLICY.md`.

### M3.11 — Link Integrity & Navigation Security — PLANNED

Hardening di M3.5/M3.6.

- risorsa target inesistente;
- fragment/anchor inesistente;
- riferimenti relativi complessi e percent-encoding;
- note/backlink rotti;
- target verso risorse non navigabili;
- link interni sempre confinati al package;
- un salto fallito non modifica `ReadingLocation` né back-stack;
- link esterni tramite allow-list esplicita;
- nessun controllo URL tramite rete;
- nessun `file:`, script, shell o schema sconosciuto avviato dal contenuto EPUB.

### M3.12 — Crash Containment & Diagnostics UX — PLANNED

Rendere esplicito il confine tra errore documento e guasto interno.

- fallimento di apertura confinato alla sessione/libro;
- cleanup delle risorse temporanee;
- stato, bookmark, highlight e note preesistenti preservati;
- ritorno alla shell/libreria quando possibile;
- messaggio primario leggibile, non stack trace;
- codice diagnostico EReader per errori interni;
- dettagli tecnici separati e disponibili per troubleshooting.

### M3.13 — Corrupted EPUB Corpus & Reliability Gate — PLANNED

Corpus sintetico di pubblicazioni volutamente danneggiate e gate deterministico.

Casi iniziali:

- container/OPF/XHTML malformati;
- capitolo/spine item/immagine mancanti;
- link e anchor rotti;
- traversal/path assoluti/duplicate entry;
- ZIP troncato;
- entry oversized / compression bomb sintetica;
- encryption non supportata;
- input Unicode/encoding problematici.

Outcome attesi per fixture:

```text
OPEN
OPEN_WITH_WARNINGS
OPEN_DEGRADED
REJECT_DOCUMENT
```

Il gate deve verificare anche che l'applicazione resti operativa, che non avvengano scritture fuori scope e che lo stato valido precedente non venga corrotto.

## M4 — Libreria gestita

### M4.0 — Managed Library — PLANNED

- aggiunta di cartelle;
- scansione controllata degli `.epub`;
- deduplicazione;
- gestione libri mancanti/spostati;
- nessun obbligo di introdurre SQLite nella prima iterazione.

### M4.1 — Library Metadata Cache — PLANNED

- cache locale di titolo/autore e metadata necessari alla libreria;
- invalidazione tramite identità/path e segnali file appropriati;
- evitare ingestione integrale ripetuta per la sola lista biblioteca.

### M4.2 — Library Sorting & Filtering — PLANNED

- titolo;
- autore;
- ultimo accesso;
- progresso derivato;
- filtri in lettura/non iniziato/terminato/file mancante;
- riuso del motore di ricerca M3.1 dove appropriato.

### M4.3 — Reading Statistics — PLANNED

- libri iniziati/completati;
- avanzamento;
- sessioni e tempo approssimativo;
- statistiche derivate separate dalla posizione autoritativa.

### M4.4 — Relocation & Library Maintenance — PLANNED

- ricollegamento di EPUB spostati;
- preservazione di progress, bookmark e annotazioni quando `BookId` conferma l'identità;
- manutenzione esplicita, non guessing automatico su libri diversi.

## M5 — Hardening esteso

### M5.0 — Large Book / Performance Hardening — PLANNED

- EPUB grandi e capitoli enormi;
- migliaia di TOC entry;
- molte immagini/bookmark/search result/annotazioni;
- profiling memoria;
- bounded collections e lazy loading solo dove preserva il modello deterministico.

### M5.1 — EPUB Compatibility Corpus — PLANNED

Ampliare il corpus con EPUB 2/3 reali e sintetici: navigation edge case, XHTML problematico ma supportabile, Unicode, RTL almeno diagnostico, SVG fallback, manifest complessi, note e anchor particolari.

### M5.2 — Diagnostics & Recovery UX Final Hardening — PLANNED

Secondo passaggio su diagnostics/recovery basato sui casi emersi dal corpus reale M5.1. M3.8–M3.13 costituiscono l'infrastruttura; M5.2 ne consolida copertura e UX prima della release candidate.

## M6 — Release

### M6.0 — Packaging & First Release Candidate — PLANNED

- publish framework-dependent/self-contained secondo matrice definita;
- Windows/Linux;
- layout ZIP pulito;
- manuale utente;
- sample config;
- smoke dal pacchetto pubblicato;
- reliability/security gate cumulativo prima della RC.

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
