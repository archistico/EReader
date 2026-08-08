# EPUB Security Model

**Stato:** hardening M3.9 Hotfix 1 e recovery M3.10 Hotfix 2 VALIDATED; link security M3.11 CANDIDATE; M3.12 resta pianificata.  
**Baseline:** M3.10 Hotfix 2 VALIDATED.

## Threat model

Un file EPUB aperto dall'utente è **input non attendibile**.

Anche un file con estensione `.epub` può contenere:

- struttura ZIP/OCF malformata;
- path costruiti per uscire dal namespace virtuale del libro;
- XML/XHTML ostile o enormemente complesso;
- riferimenti a risorse assenti o remote;
- collegamenti con schemi URI non desiderati;
- payload compressi progettati per consumare memoria, CPU o spazio;
- contenuto cifrato o deliberatamente fuori scope.

EReader non deve presupporre che il file sia benigno solo perché è stato scelto esplicitamente dall'utente.

## Garanzie già presenti prima di M3.9

La baseline corrente possiede già controlli importanti:

- nessuna estrazione generale dell'archivio EPUB sul filesystem;
- namespace OCF case-sensitive con normalizzazione controllata;
- rifiuto di traversal e separator percent-encoded nei path OCF;
- rifiuto delle duplicate entry nei contratti container già coperti;
- XML bounded con DTD proibiti e resolver esterni disabilitati ai boundary XML;
- Content Document bounded;
- limiti su nodi DOM, profondità e numero blocchi;
- ispezione bounded di `META-INF/encryption.xml`;
- cifratura reale fermata prima dell'elaborazione Content;
- nessuna decrittazione o circumvention DRM;
- nessun motore JavaScript;
- nessun retrieval di rete automatico;
- immagini raster locali aperte solo dopo azione esplicita e con payload bounded;
- hyperlink esterni delegati al sistema solo dopo azione esplicita e limitati dal contratto M3.5.

Questi punti restano invarianti da preservare nelle milestone future.

## Hardening M3.9 validato

M3.9 non sostituisce i limiti specifici dei parser; aggiunge un primo firewall a livello OCF/ZIP e chiude i failure path attesi che potevano emergere soltanto durante la decompressione.

### Budget ZIP

```text
entry archivio massime              100000
entry decompressa massima          256 MiB
totale decompresso dichiarato        2 GiB
compression-ratio controllato da     16 MiB
compression-ratio massimo           500:1
fallback OPF massime                  64
```

Questi limiti sono guardrail di sicurezza, non obiettivi di performance. I documenti effettivamente elaborati dal reader hanno limiti più stretti già esistenti: `container.xml` 1 MiB, OPF 4 MiB, navigation 4 MiB, Content Document 8 MiB, preview raster 16 MiB.

### Entry ZIP e path

M3.9 aggiunge:

- rifiuto delle entry Unix di tipo speciale dichiarate nelle external attributes ZIP, inclusi i symbolic link;
- rifiuto di prefissi drive/schema (`C:/...`, `C%3A/...`, `scheme:...`) dopo normalizzazione/decodifica controllata, oltre che nei nomi fisici ZIP;
- controllo individuale/cumulativo delle lunghezze dichiarate dalla central directory;
- rifiuto di rapporti di compressione patologici;
- `ValidatedZipEntryStream` che controlla i byte realmente letti contro la lunghezza dichiarata quando viene raggiunto EOF;
- conversione di `InvalidDataException`/compression method non supportato in `EpubContainerException` stabile.

Restano validi traversal rejection, separatori encoded rejection, duplicate entry rejection e namespace OCF case-sensitive. Non viene applicata normalizzazione Unicode distruttiva: nomi differenti restano differenti.

### Corruzione scoperta dopo il bootstrap

Una ZIP può essere abbastanza integra da leggere central directory e `container.xml`, ma fallire quando viene aperta una entry successiva. M3.9 estende `EpubPublicationValidator` affinché un `EpubContainerException` emerso durante Protection, Package, Navigation o Content venga restituito come:

```text
EpubValidationStatus.Invalid
EpubDiagnosticCategory.Container
ReaderOperationStatus.DocumentUnreadable   (via bridge M3.8)
```

Non viene introdotto un catch-all: eccezioni inattese che indicano un possibile bug EReader restano fuori da questo contratto fino a M3.12.

### URI e fallback OPF

Le risorse remote del manifest possono usare solo `http:` o `https:`. La presenza di una URI remota non autorizza alcun download: EReader resta offline durante parsing/validation/rendering.

`file:`, `data:`, `javascript:`, `ftp:` e schemi non previsti sono rifiutati. Le fallback chain mantengono cycle detection e sono ora limitate a 64 passaggi per evitare traversal CPU patologici.

### XHTML/encoding

I Content Document continuano a essere letti in memoria bounded prima di AngleSharp. M3.9 rende strict la decodifica UTF-8/UTF-16 e converte sequenze invalide in `EpubContentException.InvalidXhtml`; control character XML sotto U+0020 diversi da TAB/LF/CR sono rifiutati prima del parsing HTML.

I boundary XML veri e propri continuano a usare `XmlReader` con resolver `null`, DTD proibiti salvo il caso NCX canonico già vincolato e senza external resolution.

## Sicurezza dei link — M3.11 candidate

### Link interni

I target locali vengono risolti solo nello spazio virtuale OCF. Path relativi e percent-encoding attraversano `OcfPath`; un traversal oltre la root, un separatore codificato o un riferimento malformato non viene mai trasformato in accesso filesystem.

Nel percorso recovery-aware un target assente/non risolvibile:

```text
preserva il testo
rimuove la semantica azionabile
produce ER-EPUB-RECOVERY-LINK-001
non modifica ReadingLocation
non altera il back-stack
non cerca destinazioni fuori dal package
```

Il parser pubblico strict continua a poter rifiutare lo stesso errore: la degradazione è una decisione della facade operativa, non un allentamento dei contratti parser.

### TOC

M3.10 degradava l'intero TOC quando un target era irrisolvibile. M3.11 rende il recovery granulare: il nodo rotto produce `ER-EPUB-RECOVERY-NAVIGATION-003`; una foglia senza figli viene omessa, mentre un parent con figli validi viene mantenuto come grouping/non-navigable node. Fratelli e figli validi restano disponibili.

### Link esterni

`ExternalLinkPolicy` nel Domain definisce l'unica allow-list format-neutral condivisa dall'adapter EPUB e dall'handoff CLI:

```text
http:
https:
mailto:
```

Qualsiasi altro schema resta non azionabile. In particolare `file:`, `javascript:`, `data:`, `ftp:`, shell/interpreter o schemi sconosciuti non vengono passati a `Process.Start`. La soppressione nel percorso recovery-aware produce `ER-EPUB-SECURITY-LINK-001`.

EReader non usa rete per verificare l'esistenza o la reputazione di un URL: nessun `HttpClient`, `WebRequest`, DNS probe o fetch viene eseguito durante parsing/validation/rendering.

## Filesystem

Il contenuto EPUB non deve poter scegliere un path arbitrario di scrittura.

L'eccezione controllata corrente è il preview M3.4:

- solo una risorsa raster supportata;
- richiesta esplicitamente dall'utente;
- copiata in file temporaneo controllato dal CLI;
- cleanup tentato alla chiusura;
- path non derivato come destinazione arbitraria controllata dal libro.

## Rete

Principio:

> Nessuna rete automatica durante parsing, validation, rendering o verifica link.

Un link esterno può essere delegato al sistema solo come azione esplicita dell'utente. EReader non deve scaricare CSS, immagini, font o documenti remoti per rendere leggibile un EPUB locale.

## Resource exhaustion

M3.9 e M5.0 hanno responsabilità complementari:

- **M3.9:** guardrail di sicurezza contro input patologico;
- **M5.0:** performance hardening di libri grandi ma legittimi.

Un limite di sicurezza non deve essere confuso con un obiettivo prestazionale.

## Recovery sicura

La recovery non può indebolire il security model. Per esempio, se una risorsa interna non esiste, non è consentito cercarla liberamente sul filesystem o su Internet.

Vedi [`EPUB_RECOVERY_POLICY.md`](EPUB_RECOVERY_POLICY.md).


## M3.10 non indebolisce il security boundary

La recovery M3.10 è subordinata ai guardrail M3.9. Una `EpubContainerException` che indica corruzione, budget, ZIP feature non supportata, entry speciale o path non sicuro resta un failure documento. Solo l'assenza semplice della risorsa navigation può essere degradata a TOC vuoto.

Il percorso OPF recovery-aware non autorizza accessi a risorse mancanti: differisce esclusivamente il controllo `container.Contains(...)` per permettere al validator di classificare la risorsa. Non estrae file, non attraversa directory e non effettua network retrieval.
