# ADR-0013 — Risorse come descriptor, payload fuori dal Domain

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Un libro può contenere immagini, font, stylesheet e altre risorse. Inserire byte array, stream o path dell'archivio nel Domain collegherebbe il modello alla strategia di caricamento e aumenterebbe il costo di memoria.

EReader ha però bisogno di riferimenti stabili alle risorse per rappresentare, per esempio, un `ImageBlock`.

## Decisione

Il Domain contiene `BookResource` come descriptor con:

- `ResourceId`;
- `ResourceKind`;
- media type;
- nome opzionale;
- byte length opzionale.

Il payload non fa parte del modello M0.2.

I blocchi contenuto referenziano una risorsa tramite `ResourceId`. `Book` verifica che un `ImageBlock` punti a una risorsa esistente di kind `Image`.

## Conseguenze

- nessun `Stream`, `byte[]`, ZIP entry o path sorgente nel Domain;
- il modello resta leggero e serializzabile concettualmente;
- l'accesso effettivo ai bytes dovrà essere definito da un boundary successivo;
- una futura strategia lazy/eager per le immagini non richiederà di cambiare il significato di `ImageBlock`.

## Alternative considerate

### Payload binario dentro `BookResource`

Scartato perché forza il caricamento e la retention dei bytes nel Domain.

### Path relativo EPUB come riferimento

Scartato perché farebbe trapelare la serializzazione EPUB nel modello interno.

### Stream aperto nel Domain

Scartato perché introduce lifetime management e I/O in un modello che deve restare puro.
