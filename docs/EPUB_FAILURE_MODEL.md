# EPUB Failure Model

**Stato:** M3.8–M3.11 VALIDATED; M3.12 e M3.13 CANDIDATE STACKED.  
**Baseline:** M3.13 Hotfix 1 VALIDATED; M3.12 Crash Containment e M3.13 Reliability Corpus sono incluse nella baseline validata.

## Obiettivo

Definire quando EReader deve:

1. continuare normalmente;
2. continuare con diagnostiche;
3. degradare una singola risorsa o sezione;
4. rifiutare il documento;
5. segnalare un errore interno del reader.

L'obiettivo non è rendere valido qualunque EPUB. L'obiettivo è ottenere un comportamento **deterministico, confinato e spiegabile** anche quando l'input è difettoso.

## Classi di fallimento

### Information

Evento utile per diagnosi ma senza impatto sulla lettura.

Esempi:

- metadata opzionale assente;
- `rights.xml` presente senza evidenza di cifratura reale;
- font obfuscation riconosciuta ma irrilevante per il reader testuale.

### Warning

Anomalia che non impedisce l'uso del contenuto principale.

Esempi target:

- cover assente;
- CSS non disponibile o ignorato;
- metadata incompleti;
- risorsa non essenziale non utilizzabile.

### RecoverableError

Una parte prevista dal libro non è utilizzabile, ma esiste una recovery non ambigua.

Esempi target:

- immagine locale dichiarata ma corrotta → placeholder;
- target hyperlink mancante → testo preservato, link non azionabile, posizione/back-stack invariati + diagnostica M3.11;
- singolo Content Document non leggibile quando la policy consente di saltarlo senza inventare l'ordine di lettura.

Ogni recovery deve dichiarare esplicitamente il comportamento applicato.

### FatalDocumentError

Il documento non può essere aperto o continuato in modo affidabile.

Esempi:

- archivio non interpretabile come container EPUB supportato;
- `container.xml` non utilizzabile;
- Package Document OPF assente o non determinabile;
- nessun contenuto leggibile nello spine dopo le recovery ammesse;
- contenuto essenziale cifrato con meccanismo non supportato;
- struttura talmente ambigua da richiedere guessing.

Effetto richiesto:

```text
Book non disponibile
Reader/shell ancora operativo
Stato persistente valido preservato
Diagnostica chiara
```

### InternalError

Eccezione o violazione d'invariante non prevista dal failure model del formato.

Non deve diventare automaticamente `Invalid EPUB`.

L'UX deve:

- confinare l'operazione quando possibile;
- preservare i dati già validi;
- mostrare un codice di errore EReader;
- rendere disponibili i dettagli tecnici;
- evitare stack trace come messaggio primario.

## Errori attesi e bug

Il confine fondamentale è:

```text
Errore del documento        → diagnostica EPUB + recovery/rifiuto
Feature fuori scope         → Unsupported
Errore I/O previsto         → errore operativo esplicito
Bug/errore runtime inatteso → InternalError, non EPUB Invalid
```

Questa distinzione preserva il principio già adottato dall'ADR-0023: non nascondere bug dietro un catch-all del parser.

## Atomicità dello stato

Un fallimento durante apertura o navigazione non deve produrre uno stato parzialmente valido.

In particolare:

- non sostituire `lastBook` con un libro che non ha completato l'apertura;
- non modificare bookmark/highlight/note per effetto di un errore di parsing;
- non avanzare la posizione quando un salto hyperlink fallisce;
- non persistere pagina/riga/viewport come meccanismo di recovery;
- mantenere la scrittura atomica di `state.json`.

## Regola contro il guessing

La recovery è consentita quando il risultato è determinabile dal documento e dal contratto EReader.

Se due interpretazioni differenti sono entrambe plausibili e cambierebbero l'ordine o il contenuto letto, EReader deve preferire una diagnostica esplicita al guessing silenzioso.

## Relazione con M0.7

M0.7 classifica l'ingestione in `Valid`, `Invalid`, `Unsupported`. M3.8 preserva questo contratto e costruisce sopra di esso `ReaderDiagnostic` / `ReaderOperationSummary` nell’Application layer; il bridge EPUB resta nel CLI/composition root.

Vedi [`DIAGNOSTICS.md`](DIAGNOSTICS.md) per la tassonomia reader-wide e i codici diagnostici.


## Regole rese eseguibili in M3.10

- navigation assente/non utilizzabile + primary content valido → `Valid` con recovery diagnostic e TOC vuoto;
- primary spine failure → `Invalid`;
- supplementary `linear="no"` + expected content failure → skip con recovery diagnostic;
- immagine referenziata ma assente → `Valid` + `RecoverableError`;
- altra risorsa locale non essenziale assente → `Valid` + `Warning`;
- Container corruption/security continua a prevalere sulla recovery;
- nessun catch-all converte un bug EReader in errore documento.


## M3.12 — Containment degli InternalError

M3.12 implementa il ramo `InternalError` senza alterare la classificazione EPUB. Il solo catch generale produttivo è nel composition root CLI. Una failure contenibile produce `ER-READER-INTERNAL-001`, `InternalFailure`, action `AbortOperation` ed exit code 6.

Regole:

- `InternalFailure` non può contenere `FatalDocumentError`;
- il messaggio principale chiarisce che l'errore non dimostra che il file sia illeggibile;
- lo stato già salvato resta il riferimento persistente;
- `--diagnostics` espone i dettagli tecnici separatamente;
- `OutOfMemoryException`, `StackOverflowException` e `AccessViolationException` non vengono trattate come normali failure recuperabili;
- parser e validator EPUB restano privi di catch-all.

Vedi [`CRASH_CONTAINMENT.md`](CRASH_CONTAINMENT.md).
