# ADR-0043 — Il progresso stabile usa il contenuto logico UTF-16

- **Stato:** Accepted
- **Data:** 2026-08-08
- **Milestone:** M2.5

## Contesto

EReader mostra già un numero di pagina deterministico, ma la pagina è una coordinata di layout: cambia quando cambiano larghezza/altezza del terminale, wrapping o futura tipografia. Una percentuale calcolata da `PageNumber / PageCount` cambierebbe quindi durante un resize pur restando nello stesso punto del libro.

`ReadingLocation.CharacterOffset` è invece espresso in code unit UTF-16 sul testo logico Domain. Ricerca, anchor XHTML e navigazione persistente usano già questo spazio di coordinate.

## Decisione

M2.5 introduce `BookProgressIndex` nell'Application layer.

- L'indice segue l'ordine di `Book.ReadingOrder`.
- Ogni blocco pesa `ContentText.GetPlainText(block).Length` code unit UTF-16.
- Il progresso consumato è la somma dei blocchi precedenti più `ReadingLocation.CharacterOffset` nel blocco corrente.
- Una location a inizio sezione vale la somma del testo logico delle sezioni precedenti.
- Le sezioni `Supplementary` restano parte del `ReadingOrder` format-neutral e sono quindi incluse.
- Un libro senza testo logico ha progresso `0.0%`.
- Il valore è derivato a runtime e non viene scritto in `state.json`.
- Pagina, riga, viewport, celle terminale e grapheme non partecipano al calcolo.

`BookProgressIndex` viene costruito una sola volta per `ReaderSession`, evitando di percorrere tutto il libro a ogni refresh della TUI.

## Conseguenze

- La percentuale resta identica dopo resize/reflow alla stessa `ReadingLocation`.
- Emoji e caratteri surrogate restano coerenti con gli offset già usati dal Domain.
- La percentuale può differire dalla percentuale basata su pagine mostrata da altri reader: EReader privilegia stabilità e riproducibilità.
- Immagini o separatori senza testo logico non aggiungono unità di progresso.

## Alternative scartate

### Pagina corrente / pagine totali

Scartata: dipende direttamente dal viewport e dal wrapping.

### Numero di blocchi

Scartata: un titolo di poche lettere e un paragrafo molto lungo avrebbero lo stesso peso.

### Celle terminale dopo il layout

Scartata: introdurrebbe dipendenza dal frontend e cambierebbe con Unicode width e resize.
