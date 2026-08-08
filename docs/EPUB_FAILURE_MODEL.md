# EPUB Failure Model

**Stato:** foundation M3.8 validata; hardening input M3.9 candidate; recovery/containment M3.10–M3.13 pianificati.  
**Baseline:** M3.8 Hotfix 1 VALIDATED; M3.9 Defensive EPUB Input Security è la candidate corrente.

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
- target hyperlink mancante → posizione invariata + diagnostica;
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

Vedi [`VALIDATION_DIAGNOSTICS.md`](VALIDATION_DIAGNOSTICS.md) e [`DIAGNOSTICS.md`](DIAGNOSTICS.md).
