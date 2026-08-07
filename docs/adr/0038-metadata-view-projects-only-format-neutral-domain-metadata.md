# ADR-0038 — La vista metadata proietta solo metadata Domain format-neutral

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

M2.2 deve mostrare informazioni editoriali utili nella TUI. I dati originano dal Package Document EPUB, ma da M0.2/M0.6 il reader dispone già di `BookMetadata` neutrale. Leggere direttamente OPF o tipi `EbookReader.Epub` dalla UI reintrodurrebbe il formato sorgente nel boundary Terminal.Gui e renderebbe la schermata metadata dipendente dall'EPUB parser.

Descrizioni, identificatori e contributor possono inoltre superare la larghezza del terminale e contenere Unicode wide/emoji.

## Decisione

`ReaderSession` proietta `Book.Metadata` e `Book.Id` in una sequenza di `ReaderMetadataEntry` composta esclusivamente da label/value string neutre.

`ReaderMetadataFormatter`, senza dipendenza da Terminal.Gui, converte tali entry in righe adattate alla larghezza corrente usando la stessa misura deterministica `TerminalCellWidth` del layout.

`ReaderWindow` possiede soltanto lo stato effimero della modalità metadata e l'offset di scroll. La vista si apre con `m`; `↑/↓`, `j/k` e `PgUp/PgDn` ne controllano lo scroll. Nessuna operazione metadata modifica la `ReadingLocation` o lo stato JSON.

## Conseguenze

- nessun tipo EPUB entra nella vista metadata;
- la UI resta riutilizzabile rispetto a future sorgenti di `Book` senza modificare il Domain;
- il wrapping dei metadata è coerente con celle terminale e Unicode;
- campi opzionali mancanti vengono omessi;
- resize riformatta i metadata sulla nuova larghezza senza influire sulla posizione di lettura;
- il metadata overlay non richiede `Dialog`, `ListView` o altri widget complessi Terminal.Gui.

## Alternative considerate

### Leggere l'OPF direttamente dalla TUI

Scartata: violerebbe il boundary format-neutral e duplicherebbe la normalizzazione già svolta dall'adapter EPUB.

### Mostrare una singola stringa senza wrapping

Scartata: descrizioni e identificatori lunghi verrebbero troncati o resi illeggibili su terminali stretti.

### Persistenza dello stato della vista metadata

Scartata: apertura e offset di scroll sono coordinate UI effimere e non fanno parte della posizione di lettura durevole.
