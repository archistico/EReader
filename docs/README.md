# Documentazione EReader

Il `README.md` nella radice è il punto di partenza per chi vuole **installare e usare EReader**. Questa cartella raccoglie invece i dettagli tecnici e di manutenzione.

## Uso e comportamento

- [`READER_TUI.md`](READER_TUI.md) — funzionamento del reader fullscreen.
- [`CONFIGURATION_KEYMAP.md`](CONFIGURATION_KEYMAP.md) — preferenze e tasti configurabili.
- [`THEMES.md`](THEMES.md) — temi disponibili.
- [`SEARCH.md`](SEARCH.md) — ricerca nel libro.
- [`BOOKMARKS.md`](BOOKMARKS.md) — bookmark persistenti.
- [`HIGHLIGHTS_NOTES.md`](HIGHLIGHTS_NOTES.md) — evidenziazioni e note personali.
- [`INTERACTIVE_TOC.md`](INTERACTIVE_TOC.md) — indice interattivo.
- [`METADATA_VIEW.md`](METADATA_VIEW.md) — metadati del libro.
- [`HYPERLINKS.md`](HYPERLINKS.md) — link interni ed esterni.
- [`FOOTNOTES_ENDNOTES.md`](FOOTNOTES_ENDNOTES.md) — note EPUB.
- [`IMAGES.md`](IMAGES.md) — anteprima immagini.
- [`LOCAL_LIBRARY.md`](LOCAL_LIBRARY.md) e [`LIBRARY_SEARCH.md`](LIBRARY_SEARCH.md) — libreria recente e ricerca.

## EPUB, affidabilità e sicurezza

- [`EPUB_COMPATIBILITY.md`](EPUB_COMPATIBILITY.md) — cosa è supportato e cosa no.
- [`EPUB_FAILURE_MODEL.md`](EPUB_FAILURE_MODEL.md) — classificazione degli errori.
- [`EPUB_RECOVERY_POLICY.md`](EPUB_RECOVERY_POLICY.md) — quando EReader continua e quando rifiuta il libro.
- [`EPUB_SECURITY_MODEL.md`](EPUB_SECURITY_MODEL.md) — modello di sicurezza dell'input EPUB.
- [`DIAGNOSTICS.md`](DIAGNOSTICS.md) — diagnostica reader-wide.
- [`CRASH_CONTAINMENT.md`](CRASH_CONTAINMENT.md) — separazione fra errori del documento ed errori interni.
- [`RELIABILITY_CORPUS.md`](RELIABILITY_CORPUS.md) — corpus di EPUB validi, degradati, corrotti e ostili.

## Architettura

- [`ARCHITECTURE.md`](ARCHITECTURE.md) — panoramica dei componenti e delle dipendenze.
- [`DOMAIN_MODEL.md`](DOMAIN_MODEL.md) — modello del libro format-neutral.
- [`EPUB_CONTAINER.md`](EPUB_CONTAINER.md) — container OCF/ZIP.
- [`OPF_PACKAGE.md`](OPF_PACKAGE.md) — package OPF.
- [`NAVIGATION.md`](NAVIGATION.md) — parsing EPUB Navigation/NCX.
- [`XHTML_SEMANTIC_DOMAIN.md`](XHTML_SEMANTIC_DOMAIN.md) — conversione XHTML → Domain.
- [`DETERMINISTIC_LAYOUT.md`](DETERMINISTIC_LAYOUT.md) — layout deterministico.
- [`LOGICAL_NAVIGATION.md`](LOGICAL_NAVIGATION.md) — `ReadingLocation` e mapping layout.
- [`READING_STATE.md`](READING_STATE.md) — persistenza dello stato.
- [`STABLE_PROGRESS.md`](STABLE_PROGRESS.md) — avanzamento indipendente dal layout.
- [`RESIZE_STABILITY.md`](RESIZE_STABILITY.md) — reflow e resize.

Le decisioni architetturali autoritative sono in [`adr/`](adr/README.md). Gli ADR storici vengono mantenuti intenzionalmente: documentano perché l'architettura corrente è fatta in questo modo.

## Sviluppo e release

- [`VALIDATION.md`](VALIDATION.md) — gate cumulativo del repository.
- [`RELEASE.md`](RELEASE.md) — produzione degli archivi per GitHub Release.
- [`ROADMAP.md`](ROADMAP.md) — evoluzione prevista.
- [`PROJECT_HANDOFF.md`](PROJECT_HANDOFF.md) — stato tecnico della catena corrente.

## Documenti rimossi

Durante la pulizia documentale successiva a M3.13 sono stati eliminati documenti transitori o duplicati che non rappresentavano più una fonte utile:

- il vecchio report Graphify M1.1;
- `FIRST_READABLE_EPUB.md`, ormai assorbito dalla documentazione architetturale e dal README;
- `READER_COLORS.md`, superseded da `THEMES.md`;
- `VALIDATION_DIAGNOSTICS.md`, superseded da `DIAGNOSTICS.md` ed `EPUB_FAILURE_MODEL.md`.
