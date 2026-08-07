# ADR-0012 — Blocchi semantici lineari e albero inline limitato

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Il contenuto XHTML può avere un DOM profondamente annidato. Conservare quell'albero nel Domain obbligherebbe layout, ricerca e navigazione a conoscere strutture nate per il Web.

Al tempo stesso EReader deve preservare semantica utile come heading, emphasis, strong, citazioni, liste e link.

## Decisione

Ogni `ReadingSection` contiene una sequenza lineare di `ContentBlock` indirizzabili.

M0.2 introduce:

- `HeadingBlock`;
- `ParagraphBlock`;
- `QuoteBlock` con `Depth`;
- `ListItemBlock` con kind/depth/ordinal;
- `PreformattedBlock`;
- `ImageBlock`;
- `ThematicBreakBlock`.

La struttura inline resta un piccolo albero composto da testo, emphasis, strong, link e line break.

L'adapter EPUB/XHTML sarà responsabile della linearizzazione del DOM.

## Conseguenze

- lettura, ricerca e layout possono attraversare una sequenza deterministica;
- quote e liste annidate conservano la profondità senza esporre DOM;
- la formattazione inline resta rappresentabile;
- alcune strutture XHTML non utili al terminal reader verranno normalizzate o perse intenzionalmente;
- CSS non determina la semantica Domain.

## Alternative considerate

### Conservare il DOM AngleSharp

Scartato perché viola ADR-0003 e accoppia il Domain a XHTML/AngleSharp.

### Albero generico di block node

Scartato per M0.2 perché rende più complessi location, ricerca e layout senza un beneficio necessario per il reader CLI.

### Solo testo piatto

Scartato perché perderebbe heading, enfasi, liste, link e immagini prima che il layout possa usarli.
