# ADR-0040 — I bookmark sono stato persistente multi-book basato su ReadingLocation

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

M2.0 persisteva soltanto l'ultimo libro e la sua `ReadingLocation`. M2.4 deve aggiungere bookmark che sopravvivano a resize, reflow e riapertura del programma. Legare un bookmark a pagina/riga lo renderebbe instabile; legarlo soltanto a `lastBook` farebbe inoltre perdere i bookmark di un libro quando se ne apre un altro.

## Decisione

- Ogni bookmark persistito contiene `bookPath`, `BookId` e `ReadingLocation`.
- Nessuna coordinata di layout (`PageNumber`, `LineIndex`, viewport) entra nel bookmark.
- `state.json` passa allo schema 2 e contiene una collezione `bookmarks` multi-book separata da `lastBook`.
- Il reader continua a usare `lastBook` esclusivamente per `--resume`.
- In apertura vengono ripristinati soltanto i bookmark con stesso path, stesso `BookId` e `ReadingLocation` ancora valida.
- In salvataggio i bookmark del libro corrente sostituiscono in blocco quelli associati allo stesso path; i bookmark degli altri libri vengono preservati.
- Lo schema 1 di M2.0 resta leggibile e viene interpretato come stato senza bookmark.
- Le etichette mostrate dalla TUI sono proiezioni effimere di capitolo/testo e non vengono persistite.

## Conseguenze

- I bookmark restano stabili con resize e modifiche future del layout.
- Aprire un secondo EPUB non elimina i bookmark del primo.
- Un EPUB sostituito nello stesso path non eredita bookmark con identità editoriale vecchia.
- Il Domain rimane privo di concetti di persistenza/bookmark applicativi.
- Lo stato JSON conserva compatibilità di lettura con lo schema 1.

## Alternative considerate

### Salvare numero pagina e riga

Rifiutato: sono coordinate effimere dipendenti dal viewport.

### Salvare bookmark soltanto dentro `lastBook`

Rifiutato: aprire un altro libro cancellerebbe la raccolta precedente.

### Persistire etichette/snippet

Rifiutato per M2.4: sono dati derivabili dal `Book` corrente e possono diventare obsoleti.
