# ADR-0035 — La navigazione di una riga scorre una viewport mobile

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

M1.2 ha definito `NextLine`/`PreviousLine` come movimenti tra `ReadingLocation` associate alle righe visuali. La prima TUI M1.3/M1.4 continuava però a renderizzare l'intera `LayoutPage` fissa. Di conseguenza un movimento `↑/↓` o `k/j` all'interno della stessa pagina cambiava correttamente la posizione logica ma produceva lo stesso identico testo a schermo, apparendo all'utente come un comando non funzionante.

## Decisione

La TUI renderizza una **viewport mobile** che inizia dalla riga visuale contenente la `ReadingLocation` corrente.

- `↑/↓` e `k/j` spostano la `ReadingLocation` di una riga mappata e il testo scorre immediatamente;
- `PgUp/PgDn` continuano a usare le pagine deterministiche M1.1 come unità di salto;
- le righe sintetiche di spacing possono essere mostrate ma non diventano posizioni logiche;
- `PageNumber` resta una coordinata effimera derivata dalla riga corrente;
- nessuna coordinata della viewport mobile viene persistita.

## Conseguenze

La navigazione per riga ha feedback visivo immediato senza introdurre un cursore o una selezione artificiale. Il layout deterministico e la `ReadingLocation` restano invariati come contratti. Vicino alla fine del libro la viewport può mostrare meno righe anziché ancorarsi artificialmente all'ultima pagina: questo preserva la corrispondenza intuitiva “una pressione = uno scorrimento”.

## Alternative considerate

- **Evidenziare la riga corrente dentro una pagina fissa:** scartato perché trasformerebbe `j/k` in movimento di selezione, non in scorrimento di lettura.
- **Cambiare pagina solo quando la riga supera il bordo:** è il comportamento precedente e non fornisce feedback durante la maggior parte delle pressioni.
- **Persistire un offset di scroll separato:** scartato; `ReadingLocation` resta l'unica posizione durevole.
