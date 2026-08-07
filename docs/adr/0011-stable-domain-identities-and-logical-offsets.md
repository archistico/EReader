# ADR-0011 — Identificatori Domain tipizzati e offset logici UTF-16

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

ADR-0007 stabilisce che la posizione di lettura non può dipendere da pagina o viewport. M0.2 deve rendere concreta questa decisione e definire un contratto che ricerca, bookmark, TOC e layout possano condividere.

Usare semplici indici di sezione/blocco renderebbe la posizione fragile rispetto a trasformazioni del modello. Usare un concetto di “carattere visuale” richiederebbe inoltre logica Unicode e terminal-width dentro il Domain.

## Decisione

Il Domain usa value object distinti `BookId`, `SectionId`, `BlockId` e `ResourceId`.

`ReadingLocation` è composta da:

- `SectionId`;
- `BlockId?`;
- `CharacterOffset`.

Se `BlockId` è null, la posizione indica l'inizio della sezione e l'offset deve essere zero.

Quando `BlockId` è presente, `CharacterOffset` è l'indice UTF-16 nella stringa logica ottenuta da `ContentText.GetPlainText` per quel blocco.

## Conseguenze

- posizione indipendente da viewport e paginazione;
- ricerca .NET e location condividono la stessa unità di indice;
- emoji e altri code point supplementari possono occupare due unità di offset;
- il layout dovrà tradurre UTF-16 in grapheme/terminal cells quando necessario;
- gli adapter devono produrre identificatori deterministici per una stessa importazione.

## Alternative considerate

### Numero pagina + offset riga

Scartato perché cambia a ogni resize o variazione di layout.

### Indici numerici sezione/blocco

Scartati come identità primaria perché troppo legati alla forma corrente delle collection.

### Unicode scalar index

Semanticamente valido ma richiederebbe conversioni continue rispetto alle API `System.String` e di ricerca .NET.

### Grapheme cluster index

Scartato nel Domain perché è più vicino alla presentazione tipografica che alla posizione logica interna.
