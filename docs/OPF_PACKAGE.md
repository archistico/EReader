# M0.4 — OPF Package

## Scopo

M0.4 interpreta il Package Document indicato dal default rootfile OCF. Supporta il modello di packaging di EPUB 2.0 e EPUB 3 (`package version="3.0"`). Non interpreta ancora NCX, `nav.xhtml` o XHTML.

## Pipeline

```text
EpubContainer
   ↓
DefaultRootFile.Path
   ↓
Package Document OPF bounded XML
   ↓
metadata + manifest + spine
   ↓
EpubPackageDocument
```

`EpubPackageDocument` è un modello intermedio EPUB-specifico e rimane nell'adapter `EbookReader.Epub`.

## Metadata

Sono preservati gli elementi Dublin Core in `EpubDublinCoreMetadata`, inclusi quando presenti:

- nome DC;
- valore con whitespace collassato;
- `id`;
- `xml:lang`;
- attributi legacy OPF 2 `role`, `file-as`, `scheme`.

Sono richiesti almeno `dc:identifier`, `dc:title` e `dc:language`. L'attributo `unique-identifier` deve risolvere esattamente un `dc:identifier`.

Per EPUB 3 è richiesto esattamente un `meta property="dcterms:modified"` riferito alla pubblicazione, nel formato UTC `YYYY-MM-DDThh:mm:ssZ`.

## Manifest

Ogni `item` conserva:

- `id`;
- `href` originale;
- `media-type`;
- `properties`;
- `fallback`;
- `media-overlay`;
- risoluzione locale `OcfPath` oppure URL assoluto remoto.

Gli id devono essere univoci. Anche la risorsa risultante deve essere univoca dopo la normalizzazione dell'URL/path.

Le risorse locali devono esistere nel container. Il Package Document non può auto-includersi nel manifest. I fallback devono risolvere e non possono formare cicli.

## Spine

Lo spine è preservato nell'ordine dichiarato. Ogni `itemref` deve riferire un manifest item esistente.

`linear`:

- omesso → `yes`;
- `yes` → lineare;
- `no` → non lineare.

Deve esistere almeno un item lineare.

Sono preservati:

- `spine/@toc`, utile per EPUB 2 NCX;
- `page-progression-direction` (`default`, `ltr`, `rtl`);
- `itemref/@properties`.

## Risoluzione href

Un href locale viene risolto rispetto alla directory dell'OPF sul filesystem virtuale OCF, non tramite `System.IO.Path`.

Esempio:

```text
OPF:  EPUB/package.opf
href: ./Text/A%20B.xhtml
→     EPUB/Text/A B.xhtml
```

Sono rifiutati traversal sopra la root, separator codificati, query/fragment sugli item locali, URI `file:` e self-reference.

Gli URL assoluti remoti vengono rappresentati ma non scaricati.

## XML e limiti

- Package Document massimo: 4 MiB;
- DTD proibiti;
- resolver XML disabilitato;
- manifest massimo: 20.000 item;
- spine massimo: 20.000 itemref;
- metadata Dublin Core massimo: 10.000 entry.

## Non incluso in M0.4

- validazione/lettura NCX;
- parsing `nav.xhtml`;
- fragment/anchor navigation;
- XHTML semantic extraction;
- mapping a `Book`;
- retrieval di risorse remote;
- decisione reflowable vs fixed-layout a livello applicativo.
