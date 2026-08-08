# Diagnostics — Reliability roadmap M3.8–M3.13

**Stato:** pianificato.  
**Baseline di partenza:** M3.7 Hotfix 1 VALIDATED.  
**Contratto già esistente:** M0.7 `EpubPublicationValidator` con `Valid`, `Invalid`, `Unsupported` e codici diagnostici machine-readable.

## Principio guida

> Un EPUB può essere illeggibile. EReader no.

Un problema nel documento deve essere confinato al documento o alla risorsa interessata. Un EPUB realmente irrecuperabile può essere rifiutato, ma il processo EReader deve restare operativo, non deve corrompere lo stato persistente e deve comunicare in modo comprensibile cosa è successo.

Questa roadmap non sostituisce la diagnostica M0.7. La estende dal solo confine di ingestione all'intero ciclo di apertura, navigazione, rendering, risorse e recovery del reader.

## Tre livelli distinti

### 1. Esito di ingestione EPUB — già implementato

```text
Valid
Invalid
Unsupported
```

- `Valid`: il validator ha prodotto un `Book` format-neutral.
- `Invalid`: la pubblicazione viola il contratto strutturale supportato.
- `Unsupported`: la pubblicazione richiede una feature deliberatamente fuori scope, per esempio contenuto realmente cifrato.

Dettagli: [`VALIDATION_DIAGNOSTICS.md`](VALIDATION_DIAGNOSTICS.md).

### 2. Severità diagnostica — target M3.8

La fase M3.8 dovrà rendere uniforme una tassonomia applicativa almeno equivalente a:

```text
Info
Warning
RecoverableError
FatalDocumentError
InternalError
```

Questi nomi sono il contratto documentale di progetto; i nomi finali dei tipi C# verranno fissati durante M3.8.

- `Info`: informazione utile, nessuna degradazione.
- `Warning`: anomalia o feature ignorata; lettura possibile.
- `RecoverableError`: una parte non può essere elaborata, ma EReader applica una recovery deterministica e continua.
- `FatalDocumentError`: il libro non può essere aperto o continuato in modo affidabile.
- `InternalError`: errore inatteso del reader; non deve essere mascherato come semplice errore EPUB.

`FatalDocumentError` significa **fatale per quel documento**, non fatale per l'applicazione.

### 3. Esito UX dell'operazione — target M3.8/M3.12

L'utente deve poter distinguere almeno:

```text
SUCCESS
SUCCESS_WITH_DIAGNOSTICS
DOCUMENT_UNREADABLE
```

Un eventuale errore interno deve essere presentato separatamente come guasto EReader, con codice diagnostico e dettagli tecnici accessibili senza usare stack trace come messaggio principale.

## Contenuto minimo di una diagnostica

La diagnostica futura dovrebbe poter descrivere:

- codice stabile e machine-readable;
- severità;
- messaggio umano breve;
- fase/componente di origine;
- path virtuale OCF della risorsa, quando applicabile;
- sezione/capitolo o target logico, quando applicabile;
- eventuale azione di recovery eseguita;
- dettagli tecnici separati dalla UX primaria.

Non devono essere persistite coordinate di layout come pagina, riga o viewport.

## Esempi UX target

### Risorsa recuperabile

```text
Immagine non disponibile

La risorsa images/map.png non può essere letta.
La lettura può continuare con il placeholder testuale.

Codice diagnostico: EPUB-RESOURCE-...
```

### Documento irrecuperabile

```text
Impossibile aprire il libro

EReader non ha trovato un Package Document OPF utilizzabile e
non può determinare in modo affidabile l'ordine di lettura.

Il file EPUB non è stato modificato.
Lo stato di lettura precedente è stato preservato.

Codice diagnostico: EPUB-PACKAGE-...
```

## Regole

1. Non inventare una struttura del libro quando l'intento della pubblicazione è ambiguo.
2. Un errore recuperabile deve dichiarare quale fallback è stato applicato.
3. Un errore su link o risorsa non deve spostare implicitamente la `ReadingLocation`.
4. Un'apertura fallita non deve sostituire uno stato valido già persistito.
5. Gli errori inattesi del programma non devono essere riclassificati silenziosamente come EPUB non valido.
6. I messaggi destinati all'utente devono essere comprensibili senza conoscere OCF/OPF/XHTML; i dettagli tecnici restano disponibili separatamente.

## Milestone collegate

- **M3.8** — Diagnostics Foundation & Failure Taxonomy
- **M3.9** — Defensive EPUB Loading & Input Security
- **M3.10** — EPUB Recovery & Degraded Reading
- **M3.11** — Link Integrity & Navigation Security
- **M3.12** — Crash Containment & Diagnostics UX
- **M3.13** — Corrupted EPUB Corpus & Reliability Gate

Vedi anche:

- [`EPUB_FAILURE_MODEL.md`](EPUB_FAILURE_MODEL.md)
- [`EPUB_SECURITY_MODEL.md`](EPUB_SECURITY_MODEL.md)
- [`EPUB_RECOVERY_POLICY.md`](EPUB_RECOVERY_POLICY.md)
- [`EPUB_COMPATIBILITY.md`](EPUB_COMPATIBILITY.md)
- [`ROADMAP.md`](ROADMAP.md)
