# EReader

Lettore EPUB da terminale scritto in C# per .NET 10.

**Ultima baseline autoritativa validata:** M2.5 — Stable Progress.  
**Candidate corrente:** M3.0 — Library & Reading History.  
**Stato M3.0:** **CANDIDATE**.

M2.5 ha superato il gate completo. M3.0 è costruita esclusivamente sopra quella baseline e aggiunge una libreria recente bounded, sempre basata su `ReadingLocation` logiche e sullo stesso JSON atomico.

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
ReadingBookmarkState → JSON schema 3                    M2.4 VALIDATED
  ↓
BookProgressIndex → percentuale logica UTF-16                M2.5 VALIDATED
  ↓
ReadingHistoryState → libreria recente / --library             M3.0 CANDIDATE
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

- nessuna estrazione ZIP sul filesystem;
- nessun retrieval di rete;
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

Il gate esegue restore, build Release, suite completa, smoke CLI di help/version/info e infine legge realmente:

```text
test-books/m1.0-smoke.epub
```

La candidate M3.0 contiene staticamente **397 `[Fact]` + 16 casi `[InlineData]` su 4 `[Theory]`**, quindi sono attesi **413 casi**.

Output finale atteso:

```text
M2.5 VALIDATION PASSED
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
