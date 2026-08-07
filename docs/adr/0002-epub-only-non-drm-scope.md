# ADR-0002 — Limitare il reader a EPUB reflowable senza DRM

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Supportare contemporaneamente EPUB, PDF, MOBI, AZW3 e FB2 confonderebbe formati con modelli di layout profondamente differenti e allargherebbe troppo il primo ciclo di sviluppo.

## Decisione

Lo scope è EPUB 2 e EPUB 3 reflowable senza DRM. Gli altri formati non sono roadmap implicita: richiederebbero una decisione futura esplicita.

## Conseguenze

- parser e test possono concentrarsi su un solo ecosistema;
- il modello interno resta comunque neutrale e non assume EPUB;
- fixed-layout EPUB, scripting, media overlay e funzionalità editoriali avanzate possono essere dichiarati unsupported finché non pianificati.

## Alternative considerate

### Supporto multi-formato dall'inizio

Scartato per complessità e rischio di progettare un'astrazione prematuramente generica.

### PDF nello stesso motore

Scartato: il PDF è un formato a layout fisso con problemi differenti dal reflowable EPUB.
