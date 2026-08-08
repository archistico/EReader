# EPUB Compatibility

**Baseline autoritativa:** M3.9 Hotfix 1 VALIDATED.  
**Candidate descritta:** M3.10 — EPUB Recovery & Degraded Reading.  
**Scopo:** distinguere supporto reale, limitazioni intenzionali e hardening futuro.

## Classificazioni

La documentazione di compatibilità usa queste etichette:

```text
SUPPORTED
SUPPORTED_WITH_LIMITATIONS
IGNORED_WITH_DIAGNOSTIC
UNSUPPORTED
DOCUMENT_UNREADABLE
PLANNED_HARDENING
```

- `SUPPORTED`: parte del contratto corrente e coperta da test.
- `SUPPORTED_WITH_LIMITATIONS`: supportata entro limiti dichiarati.
- `IGNORED_WITH_DIAGNOSTIC`: elemento non essenziale che può essere ignorato senza compromettere il testo principale.
- `UNSUPPORTED`: feature deliberatamente fuori scope.
- `DOCUMENT_UNREADABLE`: il documento non può produrre un libro utilizzabile secondo il contratto corrente.
- `PLANNED_HARDENING`: comportamento da rendere più resiliente nelle milestone M3.8–M3.13/M5.x.

## Formati

| Funzione | Stato M3.10 | Note |
|---|---|---|
| EPUB 2 reflowable | `SUPPORTED` | pipeline OCF/OPF/NCX/XHTML |
| EPUB 3 reflowable | `SUPPORTED` | nav XHTML + semantic content |
| PDF | `UNSUPPORTED` | fuori scope |
| MOBI/AZW3 | `UNSUPPORTED` | fuori scope |
| FB2 | `UNSUPPORTED` | fuori scope |
| DRM/cifratura reale delle publication resources | `UNSUPPORTED` | rilevata e fermata; nessuna circumvention |
| Font obfuscation IDPF/Adobe riconosciuta | `SUPPORTED_WITH_LIMITATIONS` | il reader testuale non necessita de-obfuscation font |

## Contenuto e presentazione

| Funzione | Stato M3.10 | Note |
|---|---|---|
| Heading, paragraph, quote, pre, list | `SUPPORTED` | Domain semantico |
| Emphasis/strong | `SUPPORTED` | stile semantico TUI |
| Hyperlink interni | `SUPPORTED` | target logico + back-stack |
| Footnote/endnote `noteref` | `SUPPORTED` | ruolo format-neutral, UX M3.6 |
| Immagini raster locali JPEG/PNG/GIF/WebP | `SUPPORTED_WITH_LIMITATIONS` | preview esplicito max 16 MiB; placeholder universale |
| SVG | `SUPPORTED_WITH_LIMITATIONS` | descriptor/placeholder; non aperto dal preview raster M3.4 |
| CSS completo | `UNSUPPORTED` | non esiste browser/CSS engine completo |
| JavaScript EPUB | `UNSUPPORTED` | non eseguito |
| Audio/video EPUB3 | `UNSUPPORTED` | fuori scope corrente |
| MathML avanzato | `UNSUPPORTED` | fuori scope corrente |
| Risorse remote | `SUPPORTED_WITH_LIMITATIONS` | descrittore possibile; nessun fetch automatico |

## Navigazione e stato

| Funzione | Stato M3.10 | Note |
|---|---|---|
| TOC EPUB 2 NCX | `SUPPORTED` | gerarchico |
| EPUB 3 nav | `SUPPORTED` | TOC/page-list/landmarks nel modello intermedio |
| Anchor XHTML | `SUPPORTED` | risolti a `ReadingLocation` |
| Bookmark | `SUPPORTED` | logici e persistenti |
| Highlights/note personali | `SUPPORTED` | schema 4, coordinate UTF-16 logiche |
| Back-stack hyperlink | `SUPPORTED` | transiente, bounded |
| Pagina/riga persistita | `UNSUPPORTED` per design | geometria effimera |
| Navigation/TOC assente con primary spine valido | `SUPPORTED_WITH_LIMITATIONS` | M3.10 apre il libro senza TOC e diagnostica degraded |
| Spine supplementare `linear="no"` non leggibile | `SUPPORTED_WITH_LIMITATIONS` | M3.10 può saltarlo su expected Content failure |
| Immagine locale referenziata ma assente | `SUPPORTED_WITH_LIMITATIONS` | placeholder/alt text + diagnostica recoverable |

## Robustezza corrente e futura

La baseline M3.9 Hotfix 1 possiede i guardrail bounded/security validati; la candidate M3.10 aggiunge recovery deterministico dei failure non essenziali. Le milestone immediatamente successive non servono ad aggiungere un nuovo formato, ma a rendere **esplicito e testabile** il comportamento davanti a pubblicazioni degradate, rotte o ostili.

### M3.8–M3.13

Già implementato/validato o in candidate:

- M3.8: diagnostica uniforme reader-wide e distinzione `FatalDocumentError` / `InternalError`;
- M3.9 validata: budget ZIP, ratio guard, tipi entry speciali, path OCF difensivi, schemi manifest allow-listed, fallback bounded, decoding XHTML strict e classificazione della corruzione ZIP tardiva;
- M3.10 candidate: TOC degradabile, skip deterministico di spine `linear="no"`, risorse opzionali mancanti e immagini referenziate mancanti con diagnostica.

`PLANNED_HARDENING` residuo:

- M3.11 link integrity completa;
- M3.12 crash containment;
- M3.13 corpus di EPUB corrotti con outcome attesi.

### M5.0–M5.2

Secondo passaggio:

- performance su libri grandi ma legittimi;
- corpus di compatibilità reale più ampio;
- hardening UX e recovery sui casi scoperti sul campo.

## Fuori scope stabile

Salvo futura decisione esplicita:

- aggiramento DRM;
- browser embedded;
- esecuzione JavaScript;
- fetch automatico di contenuti remoti;
- conversione PDF/MOBI/AZW3/FB2.
