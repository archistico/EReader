# Architettura di EReader

Stato: **M3.1 Library Search — CANDIDATE**  
Baseline autoritativa: **M2.5 Stable Progress VALIDATED**  
Target: **.NET 10 / C# 14**

## 1. Obiettivo

EReader è un lettore da terminale per EPUB 2/3 reflowable senza DRM. Il progetto separa deliberatamente:

1. formato di origine;
2. modello semantico del libro;
3. casi d'uso della sessione di lettura;
4. layout e paginazione;
5. presentazione TUI.

Il Domain non conosce EPUB, XHTML, AngleSharp o Terminal.Gui.

## 2. Moduli

### EbookReader.Domain

Nucleo format-neutral implementato in M0.2.

Contiene cinque aree pubbliche:

```text
Books/
Content/
Navigation/
Reading/
Resources/
```

Il modello centrale è:

```text
Book
├── Metadata
├── ReadingOrder[]
│   └── ReadingSection
│       └── ContentBlock[]
├── TableOfContents
└── Resources[]
```

Vincoli:

- nessun `ProjectReference`;
- nessun `PackageReference`;
- nessun riferimento a EPUB/ZIP/XML;
- nessun riferimento ad AngleSharp;
- nessun riferimento a Terminal.Gui;
- nessun path o stream del file sorgente;
- nessuna pagina o viewport nel modello persistibile.

Vedi [`DOMAIN_MODEL.md`](DOMAIN_MODEL.md).

### EbookReader.Epub

Adapter di input. In M0.4 implementa già:

- apertura ZIP EPUB read-only;
- validazione bootstrap `mimetype`;
- indice OCF case-sensitive;
- `META-INF/container.xml`;
- risoluzione sicura dei rootfile;
- stream del package document di default;
- parser OPF EPUB 2/3 bounded;
- metadata Dublin Core;
- manifest e risoluzione href;
- spine e linearità;
- modello intermedio `EpubPackageDocument`.

M0.5 aggiunge NCX/nav.xhtml normalizzati; M0.6 converte i Content Document XHTML nel Domain tramite un boundary AngleSharp confinato.

Dipende solo dal Domain. Da M0.6 AngleSharp è attivo esclusivamente nel boundary `EbookReader.Epub.Content` per i Content Document XHTML.

### EbookReader.Application

Casi d'uso indipendenti dalla UI:

- apertura libro;
- sessione di lettura;
- navigazione;
- ricerca;
- bookmark;
- progresso logico stabile;
- storia e stato;
- impostazioni.

Dipende dal Domain, non dalla TUI. M2.0 aggiunge `ReadingStateSnapshot`, `ReadingStateRestore` e `JsonReadingStateStore`: la persistenza contiene soltanto coordinate logiche e usa JSON versionato con sostituzione atomica.

### EbookReader.Layout

Motore deterministico che trasforma contenuto semantico + dimensioni viewport in righe/pagine terminali. M1.1 introduce `LayoutViewport`, `DeterministicLayoutEngine`, `BookLayout`, `LayoutPage` e `VisualLine`.

È separato da Terminal.Gui affinché:

- wrapping e pagination siano testabili senza terminale reale;
- il resize ricalcoli il layout senza modificare le identità Domain;
- una futura UI differente non richieda di riscrivere il motore.

Il wrapping avviene per grapheme e celle terminale deterministiche. M1.2 aggiunge la traduzione `ReadingLocation → LayoutPosition` e la navigazione riga/pagina mantenendo la location logica stabile.

Dipende solo dal Domain.

### EbookReader.Cli

Composition root e adapter di presentazione. È l'unico modulo autorizzato a referenziare Terminal.Gui.

M1.0 aggiunge una proiezione console diretta del `Book` Domain, mantenuta da M1.3 come modalità `--plain`.

M1.3 aggiunge `ReaderSession`, `ReaderWindow`, `TerminalGuiReaderHost` e `TerminalViewportFactory`. `ReaderSession` compone Domain/Application/Layout senza conoscere Terminal.Gui; `ReaderWindow` traduce input e presenta header/body/footer senza implementare parsing, wrapping o semantica di navigazione. M1.4 rende il viewport dinamico tramite `_body.ViewportChanged`. M2.0 aggiunge il composition wiring per caricare/salvare lo stato e il comando `--resume`; `ReaderWindow` resta ignara di JSON/filesystem. M2.1 proietta il TOC Domain; M2.2 proietta `BookMetadata` tramite `ReaderMetadataEntry` e un formatter cell-aware privo di Terminal.Gui. M2.3 aggiunge `BookTextSearch` nell’Application layer: ricerca su `ContentText.GetPlainText`, risultati `ReadingLocation` e nessuna dipendenza da layout/Terminal.Gui/EPUB.

## 3. Direzione delle dipendenze

```text
                 EbookReader.Domain
                  ▲       ▲       ▲
                  │       │       │
               Epub   Application Layout
                  ▲       ▲       ▲
                  └───────┼───────┘
                          │
                     EbookReader.Cli
```

Le frecce indicano “dipende da” verso il nucleo.

## 4. Pipeline

```text
EPUB
  │
  ▼
EbookReader.Epub
ZIP → container.xml → OPF → spine/nav → XHTML
  │
  ▼
EbookReader.Domain
Book → ReadingSection → ContentBlock/InlineContent
  │
  ├──────────────► search / bookmark / progress
  │
  ▼
EbookReader.Layout
ContentBlock + viewport → visual lines/pages        M1.1
  │
  ▼
EbookReader.Cli
plain projection M1.0 / Terminal.Gui 2.x fullscreen M1.3
  │
  ▼
ReadingState JSON M2.0 (solo modalità interattiva)
```

## 5. Domain semantics fissata da M0.2

### Identità

`BookId`, `SectionId`, `BlockId` e `ResourceId` sono value object distinti.

### Reading location

```text
SectionId + BlockId? + UTF-16 CharacterOffset
```

`BlockId == null` significa inizio sezione. Il numero di pagina non è un'identità persistente.

### Blocchi

Le sezioni contengono una lista lineare di blocchi semantici. Quote e liste preservano la profondità come metadata del blocco anziché ricostruire il DOM sorgente.

### Inline

Testo, emphasis, strong, hyperlink e line break possono essere annidati. `ContentText` produce la proiezione plain-text autoritativa del blocco.

### Resource boundary

Il Domain contiene descriptor e `ResourceId`, non bytes/stream/path. La strategia di accesso al payload verrà definita successivamente fuori dal Domain.

## 6. Invarianti principali

`Book` valida:

- reading order presente;
- almeno una sezione primaria;
- identificatori univoci nel rispettivo scope;
- target TOC e link interni risolvibili;
- riferimenti immagine validi;
- offset logici entro la lunghezza del plain text.

Le collection vengono copiate in snapshot read-only.

## 7. Dipendenze esterne

- `Terminal.Gui` — solo `EbookReader.Cli`.
- `AngleSharp` 1.7.1 — attivo solo in `EbookReader.Epub`, con uso dei tipi confinato al boundary `Content`.
- `xunit.v3.mtp-v2` — solo test.

Le versioni sono centralizzate in `Directory.Packages.props`.

## 8. Sicurezza e DRM

Il progetto non implementa rimozione, aggiramento o gestione di DRM. Risorse protette/cifrate non gestibili come normale contenuto non-DRM saranno diagnosticate come unsupported.

## 9. Testing strategy

- unit test degli invarianti Domain;
- test di snapshot/immutabilità pratica;
- test della plain-text projection;
- test di target TOC/link/resource;
- architecture tests sulle dipendenze e sul source boundary;
- EPUB sintetici dalle milestone parser;
- golden tests del layout dalle milestone M1.x;
- gate cumulativo prima di promuovere ogni candidate.

Vedi anche [`VALIDATION.md`](VALIDATION.md) e gli ADR in [`adr/`](adr/README.md).

## M0.3 — OCF container boundary

`EbookReader.Epub` contiene ora il primo adapter concreto di input:

```text
File/Stream
   ↓
EpubContainer
   ├── ZIP bootstrap
   ├── OcfPath
   ├── container.xml
   └── default package stream
```

La regola architetturale resta invariata: nessun tipo OCF/EPUB attraversa verso `EbookReader.Domain`. M0.4 potrà introdurre modelli intermedi specifici dell'adapter EPUB, ma la conversione verso il Domain avverrà solo quando il contenuto XHTML sarà semanticamente interpretato.

Il container non viene estratto su filesystem. Tutta la risoluzione usa path OCF virtuali ordinal/case-sensitive, indipendenti dalle regole di path del sistema operativo host.


## M0.4 — OPF Package boundary

M0.4 aggiunge un secondo livello all'adapter EPUB senza modificare il Domain:

```text
EpubContainer
    ↓
EpubPackageReader
    ↓
EpubPackageDocument
 ├── EpubPackageMetadata
 ├── EpubManifestItem[]
 └── EpubSpineItem[]
```

`EpubPackageDocument` è deliberatamente EPUB-specifico. Non è un `Book` e non può essere usato dal Domain come contratto. Il mapping verso il modello neutrale avverrà soltanto dopo navigation e semantic XHTML extraction.

Gli href locali sono risolti nello spazio virtuale OCF e non tramite `System.IO.Path`; gli URL assoluti remoti vengono soltanto descritti, senza accesso di rete.


## M0.5 — Navigation boundary

M0.5 introduce un modello EPUB-specifico normalizzato tra OPF e Domain:

```text
EpubPackageDocument
   ↓
EpubNavigationReader
   ├── EPUB 3 nav.xhtml
   └── EPUB 2 toc.ncx
   ↓
EpubNavigationDocument
```

La selezione è strict-by-version. I target locali sono risolti nello spazio OCF e separati dal fragment. L'esistenza dell'anchor concreto nel Content Document viene verificata da M0.6 prima della proiezione nel Domain. Vedi [`NAVIGATION.md`](NAVIGATION.md).


## M0.6 — XHTML semantic boundary

```text
EpubContainer + EpubPackageDocument + EpubNavigationDocument
                       │
                       ▼
                 EpubBookReader
                       │
             AngleSharp HtmlParser
                       │
               semantic drafts
                       │
        anchor registry + link resolution
                       │
                       ▼
                      Book
```

`AngleSharp` è una dipendenza dell’adapter `EbookReader.Epub` e non del Domain. Architecture tests impongono sia l’ownership del package reference sia il confinamento dei riferimenti sorgente alla cartella `EbookReader.Epub/Content`.

Il mapping usa draft intermedi perché gli hyperlink possono puntare in avanti: prima vengono costruiti tutti i blocchi e registrati gli anchor, poi link e TOC vengono trasformati in `ReadingLocation`. Nessun DOM AngleSharp sopravvive al metodo pubblico `EpubBookReader.Read`.

`NavigationItem.Target` è nullable solo per gruppi con figli, permettendo una gerarchia editoriale senza target sintetici.

---

## M0.7 — Validation & Diagnostics boundary

M0.7 aggiunge una facade sopra l'adapter EPUB senza spostare responsabilità nel Domain:

```text
EpubPublicationValidator
        │
        ├── EpubContainer
        ├── EpubProtectionInspector
        ├── EpubPackageReader
        ├── EpubNavigationReader
        └── EpubBookReader
                ↓
              Book
```

Il risultato è `EpubValidationResult`, che distingue:

```text
Valid       EPUB supportato, Book disponibile
Invalid     input malformato/non conforme al contratto supportato
Unsupported input riconosciuto ma richiede feature fuori scope
```

Le diagnostiche sono tipi dell'adapter EPUB e **non** del Domain.

### Protection boundary

`EpubProtectionInspector` legge solo metadata OCF. Non contiene implementazioni crittografiche e non modifica i bytes delle risorse.

```text
encryption.xml
      ↓ bounded XML
ProtectedResource[]
      ├── FontObfuscation
      └── UnsupportedEncryption
```

La font obfuscation EPUB standard non viene considerata DRM perché il reader testuale non deve usare il font incorporato. La cifratura reale termina la pipeline come `Unsupported` prima del parsing XHTML.

`rights.xml` viene osservato solo come metadata informativo.


## M1.0 — First readable boundary

M1.0 non modifica il Domain né introduce layout. Il CLI riceve il `Book` già normalizzato dalla facade EPUB e lo proietta su `TextWriter`. stdout e stderr sono distinti, e gli exit code distinguono Invalid/Unsupported/I/O. Vedi [`FIRST_READABLE_EPUB.md`](FIRST_READABLE_EPUB.md) e ADR-0025/0026.

## M1.1 — Deterministic layout boundary

M1.1 implementa il layout nel solo `EbookReader.Layout`, dipendente dal Domain. Il motore usa un viewport esplicito, non legge dimensioni dalla console e non referenzia Terminal.Gui, EPUB o AngleSharp.

```text
Book / ReadingSection
        ↓
DeterministicLayoutEngine + LayoutViewport
        ↓
BookLayout → LayoutPage[] → VisualLine[]
```

Le righe conservano kind semantico e identità sorgente. Grapheme e celle wide guidano il wrapping; le pagine sono effimere e ricalcolabili. Vedi [`DETERMINISTIC_LAYOUT.md`](DETERMINISTIC_LAYOUT.md) e ADR-0027.


## M1.2 — Coordinate logiche e visuali

M1.2 mantiene due boundary distinti:

```text
Domain/Application                  Layout
ReadingLocation                    LayoutPosition
Section/Block/UTF-16 offset   →    page/line del viewport
      stabile                        effimera
```

`EbookReader.Application.Reading.LogicalReadingNavigator` dipende solo dal Domain e gestisce la semantica di capitolo. `EbookReader.Layout.LayoutLocationResolver` e `LayoutNavigator` gestiscono esclusivamente la proiezione visuale. Nessun riferimento `Application → Layout` viene introdotto.


## 9. Reader TUI M1.3

```text
Terminal.Gui
    ↓
ReaderWindow
    ↓
ReaderSession
  ↙          ↘
Layout     Application
   \          /
       Domain
```

Vincoli:

- default interattivo: `ereader <libro.epub>`;
- fallback esplicito: `ereader --plain <libro.epub>`;
- TUI vietata quando stdin/stdout sono rediretti;
- stato durevole della sessione = `ReadingLocation`;
- pagina/riga = sole coordinate del `BookLayout` corrente;
- nessun EPUB o Terminal.Gui dentro `ReaderSession`;
- nessuna logica di layout/navigation dentro `ReaderWindow`;
- resize/reflow live implementato in M1.4 senza cambiare la `ReadingLocation`.


## M2.0 Hotfix 2 — chrome TUI

`ReaderWindow` possiede due separatori puramente visuali. Il body rimane l'unica View la cui geometria entra nel `LayoutViewport`; header, separatori e footer sono chrome esterno al layout del libro.


## M2.1 — TOC interattivo

`Book.TableOfContents` resta il modello autoritativo. `ReaderSession` lo proietta in `ReaderTocEntry` mantenendo label, depth e `ReadingLocation?`; `ReaderWindow` conserva soltanto selezione e scroll offset effimeri. Nessun tipo EPUB e nessuna coordinata di layout entra nel contratto del TOC.


## M2.2 — Metadata View

`ReaderSession` proietta `Book.Metadata`/`Book.Id` in `ReaderMetadataEntry`; `ReaderMetadataFormatter` effettua wrapping in celle terminale senza dipendere da Terminal.Gui. `ReaderWindow` conserva soltanto modalità aperta/chiusa e offset di scroll effimeri. La vista non conosce OPF/EPUB e non modifica `ReadingLocation` o `state.json`.


## M2.3 — Search pre-layout

La ricerca vive in `EbookReader.Application.Search` e usa esclusivamente il modello Domain:

```text
Book.ReadingOrder
      ↓
ContentBlock
      ↓ ContentText.GetPlainText
logical UTF-16 text
      ↓ BookTextSearch
BookSearchMatch(ReadingLocation, MatchLength)
```

`ReaderSession` conserva query/result set/indice come stato effimero. `ReaderWindow` offre soltanto il prompt `/` nella status bar e traduce `n/N` in navigazione fra match. Nessuna query viene persistita; il resize non ricalcola la ricerca e conserva la stessa `ReadingLocation`.

Dettagli: [`SEARCH.md`](SEARCH.md) e ADR-0039.


## M2.4 — Bookmark logici

```text
Book + ReadingLocation
        ↓
ReaderSession bookmark set
        ↓
ReadingBookmarkState
        ↓
JsonReadingStateStore schema 2
```

I bookmark sono Application state, non Domain state. `ReaderWindow` opera soltanto sulla proiezione di `ReaderSession`; JSON e filesystem restano fuori dalla View.


## M2.5 — Stable Progress

`EbookReader.Application.Progress.BookProgressIndex` indicizza una sola volta il testo logico del `Book.ReadingOrder`. Il mapping usa `ContentText.GetPlainText(block).Length` e `ReadingLocation.CharacterOffset`, entrambi in code unit UTF-16. Il modulo non dipende da `EbookReader.Layout` e non conosce pagine, righe visuali, celle o viewport. `ReaderSession` conserva l'indice e `ReaderWindow` mostra la percentuale derivata accanto al numero pagina. `state.json` continua a persistere soltanto coordinate logiche, non la percentuale.


### Library / History M3.0 — VALIDATED

`EbookReader.Application.Library` mantiene una cronologia bounded di `ReadingHistoryEntry`, senza Terminal.Gui, EPUB o coordinate di layout. `JsonReadingStateStore` persiste le entry nello schema 3. `LibraryWindow` e `TerminalGuiLibraryHost` appartengono esclusivamente al CLI/TUI.

### Library Search M3.1

`ReadingHistorySearch` vive nello stesso Application layer della cronologia e classifica `ReadingHistoryEntry` per titolo, autore, nome file e path. Il filtro è transiente e la `LibraryWindow` delega completamente matching/ranking all'Application layer. Nessun dato di ricerca viene scritto nello schema JSON.
