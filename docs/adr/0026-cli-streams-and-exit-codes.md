# ADR-0026 — Separare stdout/stderr e stabilizzare gli exit code CLI

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

M1.0 trasforma EReader da sola foundation tecnica a comando utilizzabile. L'output del libro deve poter essere rediretto o pipe-ato senza mescolarsi con diagnostiche EPUB e problemi operativi.

## Decisione

Il contratto CLI M1.0 è:

| Exit code | Significato |
|---:|---|
| 0 | successo / help / version |
| 2 | uso non valido o file sorgente non trovato |
| 3 | EPUB `Invalid` |
| 4 | EPUB `Unsupported`, inclusa cifratura fuori scope |
| 5 | errore I/O atteso durante l'apertura/lettura |

Inoltre:

- **stdout** contiene solo output richiesto dal comando, incluso il libro;
- **stderr** contiene diagnostiche, errori di input e I/O;
- le diagnostiche EPUB mantengono il codice stabile `ER-EPUB-*` prodotto da M0.7;
- eccezioni di programmazione/runtime inattese non vengono convertite genericamente in un falso errore EPUB.

## Conseguenze

Comandi come:

```text
ereader libro.epub > libro.txt
```

producono un file testuale senza contaminazione diagnostica, mentre script e shell possono distinguere gli esiti tramite exit code.

## Alternative considerate

- stampare tutto su stdout: respinto perché rende poco affidabile piping/redirection;
- un singolo exit code non-zero: respinto perché perde la distinzione Invalid/Unsupported/I/O;
- catturare ogni `Exception`: respinto perché nasconderebbe bug.
