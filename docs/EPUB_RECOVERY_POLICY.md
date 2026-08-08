# EPUB Recovery Policy

**Stato:** M3.10 Hotfix 2 VALIDATED; estensione link-integrity M3.11 CANDIDATE.  
**Baseline autoritativa:** M3.10 Hotfix 2 VALIDATED.  
**Gate candidate:** `M3.11 HOTFIX 1 VALIDATION PASSED`.

## Principio

EReader recupera soltanto quando la continuazione è deterministica e non altera in modo ambiguo il significato del libro.

> **Un EPUB può essere illeggibile. EReader no.**

Gli esiti concettuali sono:

```text
CONTINUE
CONTINUE_WITH_WARNING
CONTINUE_DEGRADED
REJECT_DOCUMENT
INTERNAL_ERROR
```

La facade EPUB continua a esporre `Valid`, `Invalid`, `Unsupported`. Un `Valid` può contenere diagnostiche; il bridge M3.8 lo proietta in `SuccessWithDiagnostics` e usa `RecoverableError`/`Warning` per spiegare il degraded reading.

## Matrice M3.10–M3.11

| Condizione | Esito M3.10 | Recovery |
|---|---|---|
| Metadata EPUB opzionali assenti | `CONTINUE` | nessuna diagnostica: l'assenza è valida |
| Cover/CSS/altra risorsa locale non essenziale dichiarata ma assente | `CONTINUE_WITH_WARNING` | risorsa ignorata, nessuna ricerca esterna |
| TOC/Nav EPUB3 assente | `CONTINUE_DEGRADED` | `TableOfContents.Empty` + diagnostica |
| TOC/Nav EPUB3 non utilizzabile | `CONTINUE_DEGRADED` | `TableOfContents.Empty` + diagnostica |
| NCX EPUB2 assente/non utilizzabile | `CONTINUE_DEGRADED` | `TableOfContents.Empty` + diagnostica |
| Target TOC non risolvibile nel `Book` leggibile | `CONTINUE_DEGRADED` | M3.11 Hotfix 1: foglia rotta omessa; parent con figli validi mantenuto come grouping non navigabile; resto del TOC preservato |
| Immagine locale dichiarata e referenziata ma assente | `CONTINUE_DEGRADED` | `ImageBlock`/alt text preservati, preview non disponibile |
| Risorsa remota `http/https` | `CONTINUE` | descrittore possibile, nessun fetch automatico |
| Spine item `linear="no"` con `EpubContentException` attesa | `CONTINUE_DEGRADED` | sezione supplementare saltata, diagnostica |
| Spine item primary mancante/non leggibile/non supportato | `REJECT_DOCUMENT` | nessuna |
| Nessun contenuto primary leggibile | `REJECT_DOCUMENT` | nessuna |
| `container.xml` inutilizzabile | `REJECT_DOCUMENT` | nessuna |
| Package Document OPF assente/non determinabile | `REJECT_DOCUMENT` | nessuna |
| ZIP/OCF con failure di sicurezza/corruzione | `REJECT_DOCUMENT` | nessuna |
| Cifratura/DRM reale non supportata | `REJECT_DOCUMENT` / `Unsupported` | nessuna circumvention |
| Violazione traversal/path/symlink/budget M3.9 | `REJECT_DOCUMENT` | mai aggirare il guardrail |
| Eccezione inattesa EReader | non convertita da M3.10 | containment previsto in M3.12 |

## Navigation degradabile, non sintetica

M3.10 considera la navigation un aid separato dal reading order. Se navigation XHTML/NCX non è disponibile o non può essere interpretata, il reader può continuare **solo dopo** che il contenuto principale ha prodotto un `Book` valido.

La recovery non genera automaticamente un indice dallo spine. `TableOfContents.Empty` è preferito a un TOC inventato.

Le failure `EpubContainerException` durante la navigation restano irreversibili quando indicano corruzione, feature ZIP non supportata o violazioni M3.9. Soltanto la semplice `EntryNotFound` della risorsa navigation viene trattata come navigation assente.

## Spine primary e supplementare

La distinzione è quella OPF già presente nel modello:

- `linear="yes"` o attributo `linear` assente → **primary**;
- `linear="no"` → **supplementary**.

M3.10 può saltare un item supplementare soltanto per una `EpubContentException` attesa. Non cattura `EpubContainerException` di corruzione e non cattura eccezioni runtime arbitrarie.

Gli `SectionId` continuano a usare l'indice originale dello spine (`spine-000001-...`, `spine-000002-...`): saltare una sezione non rinumera le successive.

### Commit transazionale degli anchor

Ogni Content Document viene parsato contro un dizionario locale di anchor. Gli anchor sono aggiunti alla vista globale solo quando l'intera sezione è stata parsata con successo. Una sezione supplementare scartata non può quindi lasciare target parziali.

## Risorse mancanti

`EpubPackageReader.Read(...)` pubblico conserva il contratto strict: una risorsa locale manifest mancante produce `ManifestResourceNotFound`.

La facade `EpubPublicationValidator` usa invece una lettura OPF recovery-aware che differisce il controllo di esistenza delle risorse locali. Questo permette di classificare il ruolo della risorsa prima di decidere l'esito:

- risorsa di navigation → policy navigation;
- risorsa di spine → policy primary/supplementary;
- immagine effettivamente referenziata dal `Book` → `RecoverableError`;
- altra risorsa locale non essenziale → `Warning`.

Nessuna risorsa mancante autorizza EReader a cercare file omonimi sul filesystem, ad attraversare directory o ad accedere alla rete.

## Immagini

Il Domain conserva l'`ImageBlock`, il `ResourceId`, l'alt text e la caption anche quando il file immagine dichiarato nel manifest è assente. Il rendering testuale resta quindi deterministico e può mostrare il placeholder già previsto da M3.4.

M3.10 diagnostica l'assenza fisica della risorsa. La validazione del contenuto binario specifico del formato immagine non viene trasformata in un decoder grafico interno: l'anteprima esterna resta bounded e fallisce localmente se il viewer non può aprire la risorsa.

## Link: transazione logica

M3.11 completa la transazione logica dei link:

1. identificare il link;
2. validare il target;
3. solo dopo il successo aggiungere l'origine al back-stack;
4. solo dopo il successo cambiare `ReadingLocation`.

Se il target fallisce, origine e stack devono restare invariati.

## Apertura libro: commit tardivo

Le diagnostiche di recovery navigation vengono mantenute provvisorie finché il contenuto non ha prodotto un `Book` leggibile. Se il primary content fallisce, non viene mostrato un messaggio contraddittorio del tipo "la lettura continua senza TOC": resta soltanto la diagnostica irreversibile.

La persistenza TUI continua ad avvenire soltanto dopo una sessione avviata su un `Book` valido. Un documento `Invalid`/`Unsupported` non entra nel percorso `ReadValidBook(...)` e non aggiorna lo stato di lettura.

## Persistenza

Recovery e diagnostica sono indipendenti dalla geometria del terminale. M3.10 non cambia:

- `ReadingLocation`;
- state schema 4;
- config schema 1;
- bookmark;
- highlight;
- note personali.

Le annotazioni M3.7 restano validate contro `BookId` e `ReadingLocation`.

## Fuori scope M3.10

Restano esplicitamente alle milestone successive:

- recovery granulare dei singoli hyperlink/anchor rotti — inclusa nella candidate M3.11;
- crash containment di eccezioni interne inattese — M3.12;
- corpus sistematico di EPUB corrotti con expected outcome — M3.13;
- tuning performance su libri eccezionalmente grandi — M5.0.


### Recovery hyperlink M3.11

Nel percorso recovery-aware sono recuperabili a granularità di singolo link: fragment inesistente, target fuori dal reading order navigabile e riferimenti locali non validi/traversal. Il testo inline resta nel `Book`, ma non viene creato un target azionabile. Gli schemi esterni non ammessi sono soppressi con Warning; non vengono reinterpretati come link locali.

La recovery non esegue guessing: non cerca anchor simili, file omonimi, backlink alternativi o URL remoti.
