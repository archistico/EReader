# ADR-0029 — Navigazione logica separata dalla navigazione di layout

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

M1.2 introduce due famiglie di movimenti:

1. capitolo precedente/successivo e inizio/fine capitolo, definiti sul reading order del libro;
2. riga/pagina precedente-successiva, definite sulla proiezione visuale di uno specifico viewport.

Mescolare i due concetti nell'Application layer obbligherebbe `EbookReader.Application` a dipendere da `EbookReader.Layout`, mentre mettere la semantica di capitolo nel layout trasformerebbe una regola logica in presentazione.

## Decisione

La responsabilità viene separata:

- `EbookReader.Application.Reading.LogicalReadingNavigator` opera solo su `Book` e `ReadingLocation`;
- `EbookReader.Layout.LayoutLocationResolver` traduce location logiche in coordinate del layout;
- `EbookReader.Layout.LayoutNavigator` implementa movimenti per riga/pagina e restituisce sempre `ReadingLocation`;
- `LayoutPosition` (`PageNumber`, `LineIndex`) è dichiarata effimera e non può entrare nel Domain o nello stato applicativo persistibile.

Per “capitolo” M1.2 usa le `ReadingSection` con ruolo `Primary`; le sezioni `Supplementary` vengono saltate da precedente/successivo. Inizio/fine della sezione corrente restano comunque disponibili anche su sezioni supplementary.

## Conseguenze

- Application continua a dipendere soltanto dal Domain;
- Layout continua a dipendere soltanto dal Domain;
- la futura TUI comporrà i due navigator senza spostare logica nei View;
- un cambio viewport invalida `LayoutPosition` ma non la `ReadingLocation` restituita dai navigator;
- M1.4 potrà implementare resize stability usando gli stessi contratti.

## Alternative considerate

### Application → Layout

Respinto per M1.2: non è necessario e indebolirebbe la separazione tra use case logici e proiezione visuale.

### Un solo navigator nel CLI/TUI

Respinto: sposterebbe logica testabile e riutilizzabile nell'outer adapter.

### Considerare tutte le sezioni come capitoli

Respinto: `linear=no` / `Supplementary` non appartiene normalmente alla sequenza di lettura principale.
