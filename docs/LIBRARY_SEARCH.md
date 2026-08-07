# M3.1 — Library Search

M3.1 aggiunge ricerca/filtro live alla libreria recente introdotta in M3.0.

## Uso

Aprire la libreria:

```text
ereader --library
```

Nella libreria:

```text
/                 entra nel filtro
caratteri         aggiornano i risultati in tempo reale
Backspace         elimina l'ultimo grapheme
Enter             conferma il filtro e torna alla navigazione
Esc               durante l'input annulla le modifiche
Esc               con filtro attivo cancella il filtro
q                 chiude la libreria
```

Le normali combinazioni `↑/↓`, `j/k`, `PgUp/PgDn` ed `Enter` operano sull'insieme filtrato.

## Campi indicizzati

`ReadingHistorySearch` considera, in ordine di peso:

1. titolo;
2. autore;
3. nome file senza estensione;
4. path completo.

La ricerca è case-insensitive e accent-insensitive. Titolo, autore e nome file supportano match esatti/prefix/substring e una sottosequenza fuzzy deterministica. Il path completo supporta invece solo match esatti/prefix/substring: non partecipa al fuzzy-subsequence, per evitare falsi positivi prodotti da caratteri sparsi nelle directory del percorso. Una query composta da più parole richiede che ogni token trovi un match.

## Ranking

Il ranking privilegia il significato editoriale rispetto alla posizione fisica del file: un match nel titolo precede un match equivalente trovato soltanto nel path. A parità di punteggio viene mantenuto l'ordine originale della cronologia, cioè il più recente prima.

## Persistenza

La query non è persistita. `state.json` resta schema 3 e continua a contenere solo cronologia, bookmark, ultimo libro e `ReadingLocation` logiche.

## Limiti

- massimo 200 entry, ereditato da M3.0;
- query massima: 128 code unit UTF-16;
- nessun indice su disco, database o cache persistente;
- nessuna dipendenza da Terminal.Gui nell'Application layer.
