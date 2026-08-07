# M2.3 — Search pre-layout

M2.3 aggiunge la ricerca nel contenuto del libro mantenendo il principio definito fin dall'ADR-0008: **si cerca prima del layout**.

## Pipeline

```text
Book Domain
  ↓ reading order
ContentBlock
  ↓ ContentText.GetPlainText
logical UTF-16 text
  ↓ BookTextSearch
BookSearchMatch[]
  ↓
ReadingLocation
  ↓ LayoutLocationResolver
viewport corrente
```

Nessun risultato dipende da `BookLayout`, pagina, riga visuale o dimensione del terminale.

## Comandi TUI

```text
/          apre il prompt di ricerca
Enter      esegue la ricerca
Backspace  elimina l'ultimo grapheme dal prompt
Esc        annulla il prompt
n          risultato successivo
N          risultato precedente
```

Durante l'input il libro resta visibile e il prompt occupa la status bar:

```text
Cerca: monte cristo_   Enter cerca   Esc annulla
```

Dopo `Enter`, l'header mostra lo stato della ricerca:

```text
Cerca «monte cristo»: 2/7
```

Se non ci sono risultati:

```text
Cerca «monte cristo»: 0 risultati
```

Il suffisso `+` sul numero totale indica che il result set ha raggiunto il limite bounded di 10.000 match.

## Semantica

La ricerca:

- è case-insensitive con `StringComparison.OrdinalIgnoreCase`;
- attraversa tutte le `ReadingSection`, incluse quelle `Supplementary`;
- cerca dentro la proiezione logica di ogni blocco;
- può attraversare boundary inline come strong/emphasis/link perché questi non interrompono il plain text del blocco;
- conserva offset UTF-16 Domain esatti;
- include match sovrapposti;
- seleziona inizialmente il primo risultato non precedente alla posizione corrente, con wrap al primo risultato quando necessario;
- usa wrap-around anche per `n` e `N`.

Non vengono cercate stringhe artificiali introdotte dal layout, come marker visuali di liste o padding.

## Limiti

```text
Query massima     256 code unit UTF-16
Match massimi     10.000
```

Il limite dei match evita allocazioni non bounded per query molto comuni su libri grandi. `BookSearchResultSet.IsTruncated` permette alla UI di comunicarlo.

## Persistenza

La ricerca è stato effimero della sessione.

`state.json` continua a contenere soltanto:

- path del libro;
- `BookId`;
- timestamp;
- `ReadingLocation`.

Query e indice del match non vengono serializzati. Se il lettore chiude il programma dopo aver raggiunto un risultato, la `ReadingLocation` di quel punto viene naturalmente salvata dal contratto M2.0.

## Evidenziazione

M2.3 non colora o sottolinea il termine trovato. La viewport viene ancorata alla `ReadingLocation` del match, rendendolo immediatamente raggiungibile. Un eventuale highlight richiederà un contratto visuale dedicato che traduca range logici in range di celle senza contaminare il motore di ricerca.
