# ADR-0028 — Le righe visuali conservano intervalli logici sorgente

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

ADR-0007 e ADR-0011 stabiliscono che la posizione di lettura persistibile è `ReadingLocation`, espressa tramite `SectionId`, `BlockId` e offset UTF-16. M1.1 produce invece `VisualLine` e `LayoutPage` dipendenti dal viewport, ma conserva soltanto le identità di sezione/blocco.

Per M1.2 serve una trasformazione deterministica `ReadingLocation → viewport`, inclusi wrapping, emoji/grapheme, whitespace flow, hard line e testo preformattato. Ricostruire gli offset a posteriori dal testo renderizzato duplicando il wrapping sarebbe fragile.

## Decisione

Ogni `VisualLine` associata a contenuto Domain conserva:

- `SectionId`;
- `BlockId`;
- `SourceStartOffset` inclusivo;
- `SourceEndOffset` esclusivo.

Gli offset sono sempre misurati sul plain text logico del blocco in code unit UTF-16, lo stesso sistema di coordinate di `ReadingLocation`.

Le righe sintetiche di spacing non hanno intervallo sorgente. Placeholder visuali come le immagini possono rappresentare l'intero intervallo logico del blocco quando non esiste una corrispondenza carattere-per-carattere utile.

Il wrapping calcola gli intervalli mentre produce le righe; non viene introdotto un secondo algoritmo di ricostruzione.

## Conseguenze

- `LayoutLocationResolver` può localizzare una `ReadingLocation` nel layout corrente;
- emoji e grapheme restano indivisibili visualmente pur conservando offset UTF-16 logici;
- il resize potrà rifare il layout e rilocalizzare la stessa `ReadingLocation`;
- pagine e linee restano coordinate effimere;
- il modello Domain non cambia e non conosce il layout.

## Alternative considerate

### Cercare il testo della riga nel blocco dopo il wrapping

Respinto: whitespace normalizzato, prefissi di quote/liste, tab e testo ripetuto rendono l'approccio ambiguo.

### Persistire pagina e riga

Respinto: dipendono dal viewport e violano ADR-0007.

### Memorizzare offset in celle terminale

Respinto: le celle sono una coordinata visuale e cambiano con wrapping/prefix; il contratto logico resta UTF-16.
