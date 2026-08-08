# EReader

Lettore EPUB da terminale scritto in C# per .NET 10.

**Ultima baseline autoritativa validata:** M3.8 Hotfix 1 — Diagnostics Foundation & CA1859 Analyzer Alignment.  
**Gate autoritativo:** `M3.8 HOTFIX 1 VALIDATION PASSED` (08/08/2026).  
**Candidate corrente:** M3.9 Hotfix 1 — CA1859 Return-Type Analyzer Alignment.  
**Gate candidate:** `M3.9 HOTFIX 1 VALIDATION PASSED`.

### M3.9 Hotfix 1 — CA1859 Return-Type Analyzer Alignment

Correzione di compilazione senza variazioni funzionali: il metodo privato `EpubContainer.OpenValidatedEntry(...)` restituisce ora il tipo concreto `ValidatedZipEntryStream`, cioè l’unico tipo che il metodo costruisce. Questo soddisfa `CA1859` con analyzers-as-errors senza modificare API pubbliche, policy di sicurezza o comportamento M3.9.

### M3.9 — Defensive EPUB Loading & Input Security

M3.9 tratta esplicitamente ogni EPUB come input non attendibile e completa i guardrail già introdotti da M0.3–M0.7 senza cambiare Domain, `ReadingLocation`, layout o formati persistenti.

In questa candidate:

- ogni entry ZIP file è soggetta a budget individuale e cumulativo sulla dimensione decompressa dichiarata;
- entry grandi con rapporto di compressione oltre `500:1` vengono rifiutate a partire da 16 MiB decompressi;
- le entry ZIP Unix di tipo speciale, inclusi i symlink, vengono rifiutate e nessuna entry viene estratta sul filesystem;
- `ValidatedZipEntryStream` mantiene incoerenze/corruzioni di entry dentro il boundary EPUB e verifica la lunghezza dichiarata quando la entry viene letta fino a EOF;
- path ZIP e riferimenti OCF con prefisso drive/schema nel primo segmento, anche percent-encoded, vengono rifiutati oltre ai traversal già coperti;
- le risorse remote del manifest accettano soltanto `http`/`https`; `file:`, `data:`, `javascript:`, `ftp:` e schemi non previsti vengono rifiutati;
- le fallback chain OPF sono limitate a profondità 64 oltre al preesistente cycle detection;
- XHTML UTF-8/UTF-16 viene decodificato in modalità strict e i control character XML non ammessi diventano diagnostica Content attesa;
- una corruzione ZIP scoperta durante Protection/Package/Navigation/Content diventa `Invalid` con diagnostica Container, non un'eccezione framework non classificata.

M3.8 resta la foundation diagnostica validata; M3.9 non introduce ancora recovery/degraded reading né un catch-all per bug interni EReader. Questi restano rispettivamente M3.10 e M3.12.

Dettagli: [`docs/EPUB_SECURITY_MODEL.md`](docs/EPUB_SECURITY_MODEL.md), [`docs/DIAGNOSTICS.md`](docs/DIAGNOSTICS.md).

M3.5 rende azionabili gli hyperlink Domain: salti interni su `ReadingLocation`, Backspace su stack transiente e handoff esplicito di `http`/`https`/`mailto` al sistema operativo, senza network client o browser embedded.

M3.5 Hotfix 1 corregge esclusivamente il fixture `test-books/m3.5-link-smoke.epub`: `nav.xhtml` e `ch1.xhtml` sono XHTML/XML senza DOCTYPE, coerenti con il parser sicuro EReader (`DtdProcessing.Prohibit`). Nessun file produttivo sotto `src/` cambia rispetto alla candidate M3.5 originale.

## Scope autoritativo

- EPUB 2 e EPUB 3 **reflowable**;
- solo pubblicazioni **senza DRM/cifratura del contenuto**;
- nessun PDF, MOBI, AZW3 o FB2;
- Domain indipendente dal formato sorgente;
- parser EPUB separato da Domain, application layer, layout e TUI;
- Terminal.Gui 2.x confinato al CLI/TUI;
- AngleSharp confinato all'adapter EPUB Content;
- posizione logica indipendente dal layout;
- ricerca sul contenuto logico prima del wrapping;
- persistenza iniziale JSON;
- ADR autoritativi.

## Primo comando di lettura

Da M1.3 il comando naturale apre il reader fullscreen:

```text
ereader libro.epub
```

Durante lo sviluppo:

```sh
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -- libro.epub
```

La proiezione lineare M1.0 resta disponibile esplicitamente per pipe, smoke e diagnostica:

```text
ereader --plain libro.epub
```

M2.0 aggiunge il resume dell'ultima sessione interattiva:

```text
ereader --resume
```

Lo stato predefinito è `LocalApplicationData/EReader/state.json`; `EREADER_STATE_FILE` può sovrascriverne il percorso per test o uso portabile.

La TUI usa il layout M1.1 e la navigazione logica M1.2. M1.4 aggiunge il reflow live. M2.0 salva e ripristina la stessa `ReadingLocation` logica tra esecuzioni.

Dettagli TUI: [`docs/READER_TUI.md`](docs/READER_TUI.md).

Dettagli resize: [`docs/RESIZE_STABILITY.md`](docs/RESIZE_STABILITY.md).

Dettagli persistenza: [`docs/READING_STATE.md`](docs/READING_STATE.md).

M2.0 Hotfix 1 rende visibile lo scorrimento per singola riga; Hotfix 2 aggiunge i separatori TUI. Entrambe sono ora validate.

M2.1 aggiunge `t`/`Tab` per aprire un indice gerarchico interattivo; `↑/↓` o `j/k` selezionano e `Enter` salta alla `ReadingLocation` della voce. M2.1 è validata.

M2.2 aggiunge `m` per aprire la vista metadata format-neutral; `↑/↓` o `j/k` scorrono una riga e `PgUp/PgDn` una pagina. M2.2 è validata.

M2.3 aggiunge `/` per il prompt di ricerca sul testo logico; `n` e `N` navigano i risultati avanti/indietro con wrap-around. I risultati sono `ReadingLocation` e non dipendono dal layout. M2.3 Hotfix 1 è validata.

M2.4 aggiunge `b` per aggiungere/rimuovere un bookmark alla posizione corrente e `B` per aprire l'elenco persistente. I bookmark sono multi-book e non contengono pagina/riga.

M2.4 Hotfix 1 aggiunge la palette di lettura: heading azzurri/cyan, strong verdi, emphasis gialli, testo bianco e cornici/separatori grigi. I colori esistono solo nel frontend Terminal.Gui; il layout conserva esclusivamente style span semantici. Hotfix 2 applica gli schemi tramite `View.SetScheme(...)`, API effettiva di Terminal.Gui 2.4.17.

Dettagli ricerca: [`docs/SEARCH.md`](docs/SEARCH.md).  
Dettagli bookmark: [`docs/BOOKMARKS.md`](docs/BOOKMARKS.md).

M2.5 aggiunge una percentuale stabile calcolata da `ReadingLocation` e testo logico UTF-16. Pagina e percentuale sono mostrate insieme ma hanno semantica distinta: la pagina può cambiare al resize, la percentuale no.

Dettagli progresso: [`docs/STABLE_PROGRESS.md`](docs/STABLE_PROGRESS.md).

M3.0 aggiunge libreria/history locale; M3.1 aggiunge filtro ranked/fuzzy con path escluso dal fuzzy-subsequence. Entrambe sono validate.

M3.2 aggiunge `c` per ciclare i temi Semantico scuro, Carta chiara e Monocromatico. M3.3 mantiene i temi nel boundary CLI/TUI ma persiste la preferenza in un nuovo `config.json`, separato dallo stato dei libri.

M3.3 rende configurabili gli alias stampabili del reader (`j/k`, `b/B`, `t`, `m`, `/`, `n/N`, `c`, ecc.). Frecce, PgUp/PgDn, Space, Tab, Enter, Backspace, Esc e F1 restano sempre disponibili. `ereader --config-path` mostra il percorso e `ereader --init-config` crea il file predefinito senza sovrascriverne uno esistente.

M3.4 usa `Enter` nella lettura normale per aprire l’immagine raster locale alla posizione corrente nel viewer associato dal sistema. Il layout resta testuale; JPEG/PNG/GIF/WebP sono bounded a 16 MiB, mentre SVG e risorse remote non vengono avviati. M3.4 Hotfix 1 è validata.

M3.5 dà priorità al link corrente/visibile su `Enter`: i link interni saltano a una `ReadingLocation` e `Backspace` torna all’origine; `http`/`https`/`mailto` sono delegati al sistema solo dopo azione esplicita. Se non c’è link, `Enter` conserva il fallback immagine M3.4.

Dettagli temi: [`docs/THEMES.md`](docs/THEMES.md).  
Dettagli configurazione: [`docs/CONFIGURATION_KEYMAP.md`](docs/CONFIGURATION_KEYMAP.md).
Dettagli immagini: [`docs/IMAGES.md`](docs/IMAGES.md).  
Dettagli hyperlink: [`docs/HYPERLINKS.md`](docs/HYPERLINKS.md).

## Pipeline corrente

```text
.epub
  ↓
EpubContainer                         M0.3 VALIDATED
  ↓
EpubProtectionInspector               M0.7 VALIDATED
  ↓
EpubPackageReader                     M0.4 VALIDATED
  ↓
EpubNavigationReader                  M0.5 VALIDATED
  ↓
EpubBookReader                        M0.6 VALIDATED
  ↓
Book                                  format-neutral Domain
  ↓
├── BookConsoleRenderer               M1.0 VALIDATED → stdout
└── DeterministicLayoutEngine         M1.1 VALIDATED
  ↓
BookLayout → LayoutPage[] → VisualLine[]
  ↓
LayoutLocationResolver / LayoutNavigator   M1.2 VALIDATED
  ↓
ReaderSession → ReaderWindow / Terminal.Gui   M1.3 VALIDATED
  ↓
Resize → Reflow stesso ReadingLocation          M1.4 VALIDATED
  ↓
JsonReadingStateStore → resume logico            M2.0 VALIDATED
  ↓
Line scroll + separatori TUI                         M2.0 HF1/HF2 VALIDATED
  ↓
Interactive TOC → ReadingLocation                    M2.1 VALIDATED
  ↓
Metadata View → BookMetadata                            M2.2 VALIDATED
  ↓
BookTextSearch → ReadingLocation                        M2.3 VALIDATED
ReadingBookmarkState → bookmark logici multi-book              M2.4 VALIDATED
  ↓
BookProgressIndex → percentuale logica UTF-16                M2.5 VALIDATED
  ↓
ReadingHistoryState → libreria recente / --library             M3.0 VALIDATED
ReadingHistorySearch → filtro ranked libreria                    M3.1 VALIDATED
  ↓
ReaderTheme → palette semantiche Terminal.Gui                   M3.2 VALIDATED
  ↓
JsonReaderPreferencesStore → theme + printable keymap            M3.3 VALIDATED
  ↓
EpubImageResourceReader → preview raster locale bounded          M3.4 VALIDATED
  ↓
BookHyperlinkIndex → internal ReadingLocation / external OS handoff M3.5 VALIDATED
  ↓
HyperlinkRole.NoteReference → note/endnote UX                    M3.6 VALIDATED
  ↓
ReadingAnnotationState → highlight/note logici → JSON schema 4   M3.7 VALIDATED
```

La facade di ingestione resta:

```text
EpubPublicationValidator.Validate(...)
  ↓
EpubValidationResult
├── Valid       → Book disponibile
├── Invalid     → EPUB malformato/non conforme al contratto supportato
└── Unsupported → feature deliberatamente fuori scope
```

## Contratto CLI

```text
0  successo / help / version
2  uso non valido o file non trovato
3  EPUB Invalid
4  EPUB Unsupported
5  errore I/O atteso
```

Il reader non cattura genericamente bug/runtime failure inattesi.

## Sicurezza / bounded input

La catena mantiene:

- nessuna estrazione dell'archivio EPUB sul filesystem; M3.4 consente soltanto la copia temporanea esplicita di una singola risorsa raster bounded per il viewer esterno;
- nessun retrieval di rete: M3.5 può soltanto delegare esplicitamente `http`/`https`/`mailto` al sistema operativo dopo `Enter`;
- XML DTD/XXE disabilitati ai boundary XML;
- nessun JavaScript o CSS engine;
- Content Document massimo 8 MiB;
- massimo 250.000 nodi DOM;
- profondità Content massima 64;
- massimo 50.000 blocchi per Content Document;
- `encryption.xml` massimo 1 MiB;
- massimo 10.000 risorse protette;
- traversal e separator percent-encoded rifiutati;
- contenuto cifrato fermato prima di AngleSharp;
- nessuna decrittazione o circumvention DRM.

## Dipendenze

```text
Domain
  ↑
  ├── Epub          OCF + OPF + Navigation + XHTML + Validation
  ├── Application
  └── Layout       M1.1 layout + M1.2 location mapping/navigation
        ↑
       Cli          --plain M1.0 + Terminal.Gui reader M1.3
```

Dipendenze runtime:

- `Terminal.Gui` 2.4.17 — solo `EbookReader.Cli`;
- `AngleSharp` 1.7.1 — solo `EbookReader.Epub`, boundary Content.

## Requisiti

- .NET SDK 10.x compatibile con `global.json`;
- Windows, Linux o macOS.

## Validation gate

Da estrazione pulita:

### Windows

```bat
validate.cmd
```

### Linux/macOS

```sh
./validate.sh
```

Il gate M3.9 esegue **12 step**: restore, build Release, suite completa, smoke CLI help/version/foundation-info, smoke `--plain` sui libri M1.0/M3.4/M3.5/M3.6, history e config. Nessuno smoke avvia viewer o browser esterni.

La candidate M3.9 contiene staticamente **473 `[Fact]` + 5 `[Theory]` + 19 `[InlineData]`**, quindi sono attesi **492 casi**. Il conteggio autoritativo resta quello prodotto dal gate locale.

Output atteso:

```text
M3.9 HOTFIX 1 VALIDATION PASSED
```

## Documentazione

```text
docs/
  ARCHITECTURE.md
  DOMAIN_MODEL.md
  EPUB_CONTAINER.md
  OPF_PACKAGE.md
  NAVIGATION.md
  XHTML_SEMANTIC_DOMAIN.md
  VALIDATION_DIAGNOSTICS.md
  DIAGNOSTICS.md
  EPUB_FAILURE_MODEL.md
  EPUB_SECURITY_MODEL.md
  EPUB_RECOVERY_POLICY.md
  EPUB_COMPATIBILITY.md
  FIRST_READABLE_EPUB.md
  DETERMINISTIC_LAYOUT.md
  LOGICAL_NAVIGATION.md
  READER_TUI.md
  RESIZE_STABILITY.md
  READING_STATE.md
  INTERACTIVE_TOC.md
  METADATA_VIEW.md
  SEARCH.md
  BOOKMARKS.md
  READER_COLORS.md
  STABLE_PROGRESS.md
  LOCAL_LIBRARY.md
  LIBRARY_SEARCH.md
  THEMES.md
  CONFIGURATION_KEYMAP.md
  IMAGES.md
  HYPERLINKS.md
  FOOTNOTES_ENDNOTES.md
  HIGHLIGHTS_NOTES.md
  ROADMAP.md
  VALIDATION.md
  PROJECT_HANDOFF.md
  adr/
```

Gli ADR in [`docs/adr`](docs/adr/README.md) sono il registro autoritativo delle decisioni architetturali.

## Licenza

Non è ancora stata scelta una licenza. I riferimenti esterni sono usati per studiare standard, UX e problemi del dominio; il codice di EReader è un'implementazione originale.



## M3.0 — Library & Reading History

M3.0 aggiunge una libreria locale bounded basata esclusivamente sui libri realmente aperti.

```text
ereader --library   # selezione fullscreen
ereader --history   # elenco testuale
ereader --resume    # ultimo libro globale
```

La cronologia conserva soltanto metadata format-neutral e `ReadingLocation`; non salva pagina, riga, viewport o percentuale.


## M3.1 Hotfix 1 — Library Search

M3.1 aggiunge filtro fuzzy live a `ereader --library`. Premi `/`, digita titolo/autore/path e i risultati vengono aggiornati mentre scrivi. `Enter` applica il filtro; `Esc` durante l'input annulla, mentre `Esc` con un filtro già applicato lo cancella.

La policy di ranking vive in `EbookReader.Application.Library.ReadingHistorySearch`, non nella View Terminal.Gui. La query non viene persistita. Vedi [`docs/LIBRARY_SEARCH.md`](docs/LIBRARY_SEARCH.md).

## M3.6 — Footnotes / Endnotes UX

M3.6 riconosce `epub:type="noteref"` al solo boundary EPUB e lo traduce in `HyperlinkRole.NoteReference`. Nel reader il rimando viene indicato come `NOTA`; `Enter` apre il target logico e `Backspace` torna immediatamente al testo usando lo stack transiente validato in M3.5. Nessun popup, pagina/riga o stato nota viene persistito.

Il gate M3.6 aggiunge lo smoke `test-books/m3.6-notes-smoke.epub` e non avvia browser o viewer esterni.


Audit statico M3.6: **444 Fact + 4 Theory + 16 InlineData = 460 casi attesi**.

Documentazione decisionale: **50 ADR** totali con ADR-0050 per M3.6.


## M3.6 Hotfix 1 — Help contract alignment

Hotfix 1 riallinea il test `HelpDocumentsReadableEpubCommand` alla formulazione help M3.6 `Enter segue link/rimando nota corrente`. Nessun file produttivo sotto `src/` cambia rispetto alla candidate M3.6.

Nota temi: il tema scelto è persistito in `config.json`. `semantic-dark` usa heading cyan, strong verde e emphasis giallo; `monochrome` usa intenzionalmente bianco/grigio e stili Bold/Italic. In TUI premere `c` per ciclare i temi oppure usare `ereader --config-path` per individuare la configurazione.


## M3.7 — Highlights & Personal Notes

M3.7 evolve `state.json` a schema 4 e aggiunge annotazioni logiche book-scoped. F2 evidenzia la riga logica corrente, F3 modifica una nota personale alla posizione esatta e F4 apre l'elenco annotazioni. Le coordinate restano `ReadingLocation`/UTF-16; nessuna pagina, riga o viewport viene persistita.

Audit statico baseline validata: **454 Fact + 4 Theory + 16 InlineData = 470 casi attesi**.

Documentazione decisionale: **51 ADR numerati** con ADR-0051 per il modello annotazioni/schema 4.

## Reliability e sicurezza — fase M3.8–M3.13

Prima di M4.0 la roadmap introduce un blocco dedicato a diagnostica, input EPUB non attendibile, recovery controllata, link integrity, crash containment e corpus di EPUB corrotti.

Principio guida:

> **Un EPUB può essere illeggibile. EReader no.**

Un documento realmente irrecuperabile può essere rifiutato, ma EReader deve restare operativo, preservare lo stato valido precedente e comunicare chiaramente il motivo. Gli errori recuperabili devono degradare soltanto la risorsa o parte interessata, senza guessing silenzioso.

Documenti:

- [`docs/DIAGNOSTICS.md`](docs/DIAGNOSTICS.md)
- [`docs/EPUB_FAILURE_MODEL.md`](docs/EPUB_FAILURE_MODEL.md)
- [`docs/EPUB_SECURITY_MODEL.md`](docs/EPUB_SECURITY_MODEL.md)
- [`docs/EPUB_RECOVERY_POLICY.md`](docs/EPUB_RECOVERY_POLICY.md)
- [`docs/EPUB_COMPATIBILITY.md`](docs/EPUB_COMPATIBILITY.md)
- [`docs/ROADMAP.md`](docs/ROADMAP.md)
