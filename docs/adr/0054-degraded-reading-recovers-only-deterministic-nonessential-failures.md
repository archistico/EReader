# ADR-0054 — Degraded reading recovers only deterministic non-essential failures

- **Status:** Accepted — validated with M3.10 Hotfix 2
- **Date:** 2026-08-08

## Context

M3.8 ha introdotto la tassonomia reader-wide e M3.9 ha reso l'input EPUB virtuale, bounded e difensivo. Il reader deve ora distinguere tra una pubblicazione realmente illeggibile e una pubblicazione il cui reading order principale resta affidabile anche se navigation aid o risorse non essenziali sono mancanti/difettosi.

Un semplice `catch` e continua sarebbe pericoloso: potrebbe nascondere corruzione del contenuto primario, produrre `ReadingLocation` ambigue o indebolire i guardrail di sicurezza M3.9.

## Decision

M3.10 introduce recovery soltanto quando la continuazione è deterministica e il `Book` format-neutral può essere costruito senza inventare contenuto.

### Navigation

Un Navigation Document/NCX assente o non utilizzabile non è più sufficiente da solo a rifiutare il libro. Se il reading order è valido:

- il `Book` viene costruito con `TableOfContents.Empty`;
- viene emessa una diagnostica recoverable stabile;
- nessun TOC sintetico viene inventato.

Le failure Container di sicurezza/corruzione diverse da una semplice entry mancante restano fatali anche se emergono durante la lettura della navigation.

### Spine

Gli item `linear="yes"` o implicitamente lineari restano essenziali. Se uno di essi non può essere trasformato in contenuto Domain affidabile, il documento è `Invalid`.

Un item `linear="no"` può invece essere saltato quando produce una `EpubContentException` attesa. Gli identificatori delle sezioni restano derivati dall'indice originale dello spine, quindi il recovery non rinumera le sezioni successive.

Il parsing di ogni sezione è transazionale rispetto agli anchor: gli anchor vengono aggiunti all'insieme globale solo dopo il parsing completo della sezione.

### Risorse locali non essenziali

Il validator M3.10 usa una lettura OPF recovery-aware che consente di analizzare il package anche se una risorsa locale del manifest è fisicamente assente. Il reader pubblico `EpubPackageReader.Read(...)` resta strict e conserva il contratto precedente.

Dopo che il `Book` è stato costruito:

- immagine referenziata ma assente → `RecoverableError`, placeholder/alt text, nessun fetch;
- CSS, cover o altra risorsa locale non essenziale assente → `Warning`;
- risorse usate dallo spine o dalla navigation sono gestite dai rispettivi boundary e non ricevono diagnostiche duplicate.

### Commit delle diagnostiche

Una diagnostica di recovery viene resa visibile solo se l'apertura del libro termina con un `Book` valido. Se dopo una navigation degradata il contenuto primario risulta illeggibile, la diagnostica provvisoria di recovery viene scartata e resta soltanto l'esito irreversibile.

### Security boundary

M3.10 non cattura eccezioni runtime arbitrarie e non trasforma `EpubContainerException` di corruzione/sicurezza in recovery benigno. Il crash containment di errori interni EReader resta responsabilità di M3.12.

## Consequences

- un EPUB senza TOC può essere letto se lo spine principale è affidabile;
- contenuti supplementari difettosi non impediscono necessariamente la lettura del testo principale;
- immagini mancanti degradano visivamente ma non distruggono il testo o le `ReadingLocation`;
- risorse opzionali mancanti producono diagnostica esplicita invece di un rifiuto globale;
- primary spine, package/container essenziali e guardrail M3.9 restano non recuperabili;
- nessuna risorsa viene cercata fuori dall'archivio e nessun contenuto viene sintetizzato.

## Alternatives considered

### Rendere permissivo `EpubPackageReader.Read(...)` per tutti i caller

Rifiutato: romperebbe il contratto strict usato dai test e dagli adapter che richiedono un package integralmente presente. La recovery appartiene alla facade di ingestione, non al parser OPF di base.

### Saltare anche capitoli primary corrotti

Rifiutato: il reading order cesserebbe di rappresentare in modo affidabile il libro e la perdita potrebbe essere silenziosamente sostanziale.

### Generare automaticamente un TOC dallo spine

Rifiutato: sarebbe guessing. M3.10 preferisce un libro leggibile senza TOC a una navigation inventata.

### Considerare ogni failure navigation come fatal

Rifiutato: la navigation è un aid separato dal reading order e il Domain supporta già un `TableOfContents.Empty` valido.
