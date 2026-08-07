# ADR-0039 — La ricerca opera sul testo logico Domain prima del layout

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

M2.3 introduce la ricerca full-book. Cercare sulle `VisualLine` o sulle stringhe già wrappate renderebbe i risultati dipendenti dalla larghezza del terminale: una frase potrebbe essere spezzata in punti diversi a 40, 80 o 120 colonne e un resize potrebbe modificare sia il numero sia la posizione dei match.

Il Domain dispone già di `ContentText.GetPlainText(ContentBlock)` e di `ReadingLocation` con offset UTF-16 stabili. Questi sono quindi il boundary corretto per una ricerca indipendente da EPUB e dal rendering.

## Decisione

`BookTextSearch` vive in `EbookReader.Application.Search` e attraversa il `Book.ReadingOrder` in ordine editoriale. Per ogni `ContentBlock` cerca sulla proiezione restituita da `ContentText.GetPlainText` usando confronto `OrdinalIgnoreCase`.

Ogni match è rappresentato da `BookSearchMatch` con:

- `ReadingLocation` del primo code unit UTF-16 del match;
- lunghezza del match nello stesso spazio UTF-16.

La ricerca non dipende da `BookLayout`, `VisualLine`, Terminal.Gui o tipi EPUB. I risultati sono bounded a 10.000 e la query a 256 code unit UTF-16.

`ReaderSession` conserva query, result set e indice corrente come stato effimero della sessione. `/` apre un prompt inline nella status bar; `Enter` esegue la ricerca; `n` e `N` ciclano avanti/indietro con wrap-around. Il primo risultato selezionato è il primo match non precedente alla `ReadingLocation` corrente, oppure il primo match del libro se la ricerca deve fare wrap.

La query e l'indice del risultato non vengono persistiti in `state.json`. Viene persistita soltanto la `ReadingLocation` eventualmente raggiunta tramite ricerca.

## Conseguenze

- i risultati sono indipendenti da viewport, wrapping e resize;
- una frase che attraversa `TextRun`, `StrongSpan`, `EmphasisSpan` o altri container inline resta ricercabile perché la ricerca avviene dopo la proiezione logica del blocco;
- `n/N` spostano soltanto la `ReadingLocation`, riutilizzando il layout già esistente per la visualizzazione;
- la TUI non implementa alcun algoritmo di ricerca;
- i match sovrapposti sono ammessi;
- in M2.3 non viene introdotta evidenziazione grafica del termine: il match viene portato nella viewport e lo styling resta una responsabilità distinta;
- ricerche estremamente frequenti possono essere ottimizzate in futuro con un indice, senza cambiare il contratto `BookSearchMatch`.

## Alternative considerate

### Ricerca sulle `VisualLine`

Scartata: rende i risultati dipendenti dal layout e viola ADR-0008.

### Ricerca direttamente sugli XHTML EPUB

Scartata: reintrodurrebbe EPUB nell'Application/TUI e produrrebbe coordinate non Domain.

### Persistenza della query e del match corrente

Scartata: sono stato UI/sessione, non posizione di lettura durevole.

### Evidenziazione del match in M2.3

Rinviata: richiede una proiezione di styling/range visuali distinta dalla ricerca logica e non è necessaria per validare il contratto pre-layout.
