# M0.5 — EPUB Navigation

## Scopo

M0.5 normalizza la navigazione primaria di EPUB 2 e EPUB 3 senza contaminare il Domain con NCX o XHTML.

```text
EpubContainer
    ↓
EpubPackageDocument
    ↓
EpubNavigationReader
    ├── EPUB 3 nav.xhtml
    └── EPUB 2 toc.ncx
            ↓
EpubNavigationDocument
    ├── TableOfContents
    ├── PageList?      (EPUB 3)
    └── Landmarks?     (EPUB 3)
```

## EPUB 3

La sorgente è il singolo manifest item con proprietà `nav` e media type `application/xhtml+xml`.

Sono interpretati:

- `nav epub:type="toc"` — obbligatorio e unico;
- `nav epub:type="page-list"` — opzionale e unico;
- `nav epub:type="landmarks"` — opzionale e unico;
- gerarchie `ol > li > (a|span) [+ ol]`;
- heading diretto opzionale del `nav`;
- testo label collassato in modo deterministico;
- `img` nei label tramite `alt`, poi `title`;
- `epub:type` del link/span preservato in `EpubNavigationNode.Types`.

I `span` sono nodi di raggruppamento senza target e devono avere figli.

## EPUB 2 / NCX

La sorgente è determinata da `spine/@toc`, che deve risolvere a un manifest item `application/x-dtbncx+xml` locale.

Sono interpretati:

- namespace NCX 2005;
- `version="2005-1"`;
- `navMap` unico e non vuoto;
- `navPoint` ricorsivi;
- `id` univoci;
- primo `navLabel/text` come label normalizzato;
- `content/@src` come target;
- `playOrder` non viene usato per determinare l'ordine di lettura; senza DOCTYPE può essere omesso secondo EPUB 2.0.1; con il DOCTYPE canonico è richiesto su ogni `navPoint`.

Il DOCTYPE NCX canonico NISO/DAISY può essere presente. L'identificatore PUBLIC/SYSTEM viene verificato, gli internal subset sono rifiutati e `XmlResolver` resta nullo: la DTD esterna non viene recuperata.

## Target

Un target normalizzato contiene:

```text
Href originale
LocalPath (OcfPath)
Fragment opzionale
```

Per i navigation aid EPUB 3 supportati, il path deve corrispondere a un top-level content document derivato dallo spine o da una fallback chain. Per NCX deve risolvere a un top-level Content Document derivato dallo spine o dalla relativa fallback chain.

Esempio:

```text
nav.xhtml
href = "Text/ch01.xhtml#section%201"

↓

LocalPath = EPUB/Text/ch01.xhtml
Fragment  = section 1
```

M0.5 conserva path + fragment senza aprire i Content Document. **M0.6 completa il contratto**: `EpubBookReader` verifica gli `id` reali e converte i target in `ReadingLocation` logiche prima che il contenuto raggiunga il Domain.

## Sicurezza e limiti

- massimo documento navigation/NCX: 4 MiB;
- massimo 20.000 nodi;
- profondità massima: 64;
- label massima: 16.384 caratteri;
- nessuna estrazione su filesystem;
- nessun network retrieval;
- URL assoluti rifiutati nei navigation aid supportati;
- query locali rifiutate;
- traversal e separator percent-encoded rifiutati tramite `OcfPath`;
- DTD proibiti in `nav.xhtml`;
- solo DOCTYPE NCX canonico ammesso, senza internal subset e senza resolver esterno; espansione di entità bounded come difesa aggiuntiva.

## Evoluzione dopo M0.5

M0.5 si fermava alla normalizzazione della navigation. Le milestone successive hanno poi aggiunto verifica degli anchor nel contenuto, conversione verso `EbookReader.Domain.Navigation`, parsing semantico, layout e TUI. M3.10 interviene quindi su un contratto di navigazione già completo e ne definisce il comportamento di recovery, non su un parser isolato.

## M3.10 — Navigation degradabile

La navigation non è più un prerequisito assoluto quando il reading order principale è sano. Navigation assente o non utilizzabile produce una diagnostica recuperabile e `TableOfContents.Empty`; un TOC sintatticamente valido ma con target non risolvibili viene scartato integralmente, senza costruire un indice parzialmente ambiguo. Corruzioni del contenitore sottostante restano fatal document errors.


## M3.11 — Target TOC degradabili granularmente

M3.11 restringe la recovery della navigation alla singola destinazione difettosa. Un nodo con path/fragment non risolvibile produce `ER-EPUB-RECOVERY-NAVIGATION-003`: se è una foglia viene omesso; se contiene figli validi resta nel `TableOfContents` come grouping node non navigabile (`Target == null`) e conserva label e figli; un grouping che, dopo la recovery dei discendenti, resta senza target e senza figli viene omesso. Fratelli e discendenti con target validi restano azionabili.

Il parser strict resta invariato: la granularità M3.11 appartiene al percorso recovery-aware della facade di validazione. Nessun target viene cercato fuori dal package e nessun anchor simile viene indovinato.
