# M1.0 — First Readable EPUB

## Obiettivo

M1.0 è la prima milestone in cui EReader legge realmente un EPUB da percorso locale e produce testo leggibile:

```text
ereader libro.epub
```

Non è ancora una TUI e non implementa paginazione. Lo scopo è provare end-to-end la pipeline EPUB→Domain→CLI prima di introdurre il layout dipendente dal terminale.

## Pipeline

```text
file locale
   ↓
EpubPublicationValidator
   ↓
Valid / Invalid / Unsupported
   ↓ Valid
Book Domain
   ↓
BookConsoleRenderer
   ↓
stdout
```

Il renderer non conosce EPUB, OPF, NCX, XHTML o AngleSharp: riceve solo il `Book` format-neutral.

## Proiezione dei blocchi

| Domain | Output M1.0 |
|---|---|
| `HeadingBlock` | testo + separazione verticale |
| `ParagraphBlock` | plain text + separazione verticale |
| `QuoteBlock` | prefisso `> ` per profondità |
| `ListItemBlock` unordered | `- ` + indentazione |
| `ListItemBlock` ordered | `N. ` + indentazione |
| `PreformattedBlock` | testo preservato |
| `ImageBlock` | `[Immagine: alt — caption]` oppure `[Immagine]` |
| `ThematicBreakBlock` | `---` |

Strong, emphasis e hyperlink vengono proiettati come testo visibile tramite `ContentText`. L'attivazione dei link appartiene a milestone successive.

## Cosa M1.0 non fa

- nessun wrapping alla larghezza del terminale;
- nessuna viewport;
- nessuna pagina;
- nessun input interattivo durante la lettura;
- nessun Terminal.Gui fullscreen;
- nessuna persistenza della posizione;
- nessuna ricerca.

Questi elementi iniziano da M1.1/M1.2/M1.3.

## Diagnostica e stream

Il libro viene scritto su stdout. Diagnostiche e problemi vengono scritti su stderr.

Exit code:

```text
0  successo
2  uso/path
3  EPUB Invalid
4  EPUB Unsupported
5  I/O
```

## Smoke EPUB

`test-books/m1.0-smoke.epub` è una pubblicazione EPUB 3 minimale, non DRM, usata dal gate per provare realmente il percorso:

```text
file .epub → OCF → OPF → Navigation → XHTML → Domain → CLI
```

Il validation script esegue il reader sul file dopo build e test.

## Criterio di completamento

M1.0 è **VALIDATED**: il gate cumulativo M0.5→M1.0 è passato integralmente il 07/08/2026 con 271/271 casi e smoke CLI exit code 0.
