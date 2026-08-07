# EPUB Container — M0.3

## Scopo

M0.3 implementa il bootstrap OCF necessario a trasformare un file `.epub` in un contenitore read-only dal quale le milestone successive potranno leggere il Package Document.

Non vengono ancora interpretati OPF, manifest, spine, NCX, `nav.xhtml` o XHTML.

## Pipeline

```text
file/stream EPUB
    ↓
local ZIP header #1
    ↓
verifica mimetype
    ↓
ZipArchive read-only
    ↓
indice OCF case-sensitive
    ↓
META-INF/container.xml
    ↓
rootfiles[]
    ↓
DefaultRootFile = rootfiles[0]
    ↓
stream del package document
```

## API principale

```text
EpubContainer.Open(path)
EpubContainer.Open(stream, leaveOpen)

EpubContainer.RootFiles
EpubContainer.DefaultRootFile
EpubContainer.EntryPaths
EpubContainer.Contains(OcfPath)
EpubContainer.OpenEntry(OcfPath)
EpubContainer.OpenDefaultPackageDocument()
```

`EpubContainer` è `IDisposable` perché mantiene aperti stream e `ZipArchive`.

## `mimetype`

La milestone controlla prima dell'apertura logica dell'archivio che la prima entry fisica sia `mimetype` e che:

- il metodo di compressione sia `stored` (`0`);
- il local header non abbia extra field;
- non sia marcata come ZIP-encrypted;
- `version needed to extract` sia 10, 20 o 45;
- il payload sia esattamente `application/epub+zip` senza whitespace o BOM.

## `container.xml`

Percorso richiesto:

```text
META-INF/container.xml
```

Il parser XML usa:

- `DtdProcessing.Prohibit`;
- `XmlResolver = null`;
- limite di 1 MiB;
- namespace OCF `urn:oasis:names:tc:opendocument:xmlns:container`;
- `container@version = 1.0`;
- esattamente un `rootfiles`;
- almeno un `rootfile`;
- `rootfile@media-type = application/oebps-package+xml`.

I rootfile multipli sono conservati; il primo è il default.

## Path OCF

`OcfPath` non è un path del sistema operativo.

Esempio:

```text
ZIP entry:       EPUB/My Book.opf
container.xml:   EPUB/My%20Book.opf
OcfPath:         EPUB/My Book.opf
```

I riferimenti provenienti da `META-INF` vengono percent-decoded per segmento e dot-normalized.

```text
EPUB/Temp/../package.opf
→ EPUB/package.opf
```

Non è ammesso uscire dalla root:

```text
../package.opf
→ InvalidContainerPath
```

Non è ammesso nascondere un separatore dentro percent encoding:

```text
EPUB%2Fpackage.opf
→ InvalidContainerPath
```

Le entry ZIP reali non vengono percent-decoded: `%20` può essere parte letterale del nome di un file ZIP.

## Case sensitivity

I path OCF sono case-sensitive. Quindi:

```text
EPUB/package.opf
```

è diverso da:

```text
epub/package.opf
```

anche quando EReader gira su Windows.

## Nessuna estrazione

Il parser non esegue `ExtractToDirectory` e non crea directory temporanee. Questo rende il comportamento indipendente dal filesystem host e impedisce che un path ZIP malevolo scriva fuori da una directory di destinazione.

## Error model

Gli errori di struttura vengono esposti tramite `EpubContainerException.ErrorCode`, ad esempio:

```text
InvalidZip
MimeTypeNotFirst
MimeTypeCompressed
InvalidMimeTypeContent
MissingContainerXml
InvalidContainerXml
InvalidContainerPath
DuplicateContainerEntry
InvalidRootfileMediaType
RootfileNotFound
```

Il codice chiamante deve basarsi sull'enum e non sul testo localizzato del messaggio.

## Boundary con M0.4

M0.3 termina quando può restituire in modo affidabile lo stream del package document predefinito.

M0.4 partirà da:

```text
container.OpenDefaultPackageDocument()
```

per interpretare metadata, manifest e spine OPF.
