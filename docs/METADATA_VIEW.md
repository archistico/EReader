# M2.2 — Metadata View

M2.2 rende consultabili nella TUI i metadata già presenti nel `BookMetadata` format-neutral.

## Obiettivo

La vista metadata non interpreta OPF, Dublin Core o attributi EPUB. Tutta la normalizzazione dal formato sorgente è già terminata quando `ReaderWindow` riceve il `Book` tramite `ReaderSession`.

```text
EPUB / OPF
   ↓ adapter M0.4/M0.6
BookMetadata Domain
   ↓
ReaderSession
   ↓ ReaderMetadataEntry[]
ReaderMetadataFormatter
   ↓ linee terminal-cell-aware
ReaderWindow
```

## Comandi

```text
m                 apre/chiude metadati
↑ / k             scorre una riga verso l'alto
↓ / j             scorre una riga verso il basso
PgUp / PgDn       scorre una pagina
Esc               chiude la vista metadata
t / Tab           passa all'indice
F1 / ?            passa all'aiuto
q                 esce dal reader
```

Aprire, scorrere o chiudere i metadata non modifica la `ReadingLocation`.

## Campi visualizzati

Quando presenti nel Domain:

- titolo;
- sottotitolo;
- contributor con ruolo (`Autore`, `Curatore`, `Traduttore`, `Illustratore`, `Narratore`, `Contributore`);
- `SortName` del contributor, se disponibile;
- lingue;
- editore;
- identificatori con eventuale schema;
- argomenti;
- diritti;
- descrizione;
- `BookId` format-neutral.

I campi opzionali assenti non producono righe vuote artificiali.

## Wrapping

`ReaderMetadataFormatter` è indipendente da Terminal.Gui e usa `TerminalCellWidth` per rispettare la larghezza in celle terminale, inclusi grapheme Unicode, CJK ed emoji.

Il wrapping dipende soltanto dalla larghezza corrente del body. Dopo un resize la vista viene riformattata e l'offset di scroll viene clampato al nuovo numero di righe.

## Boundary architetturale

`ReaderWindow` non conosce:

- `EpubPackageDocument`;
- `dc:*`;
- OPF;
- NCX/nav.xhtml;
- AngleSharp.

M2.2 consuma esclusivamente la proiezione neutrale prodotta da `ReaderSession`.

La vista metadata è stato UI effimero: non entra in `state.json` e non altera il contratto M2.0.
