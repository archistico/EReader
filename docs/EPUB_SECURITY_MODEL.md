# EPUB Security Model

**Stato:** parte già implementata + hardening pianificato M3.9/M3.11/M3.12.  
**Baseline:** M3.7 Hotfix 1 VALIDATED.

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

## Garanzie già presenti in M3.7

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

## Hardening M3.9 pianificato

M3.9 deve auditare e, dove necessario, introdurre test/guardrail espliciti per:

- ZIP corrotto o troncato;
- local/central directory incoerenti;
- entry duplicate o nomi ambigui;
- path assoluti e traversal in tutte le risoluzioni, non solo nei casi già coperti;
- rapporti di compressione patologici;
- dimensione decompressa individuale e cumulativa;
- numero eccessivo di entry;
- `container.xml` / OPF / navigation / XHTML con budget coerenti;
- URI e percent-encoding patologici;
- risorse dichiarate fuori dal package;
- fallback chain cicliche o eccessive;
- input Unicode anomalo senza introdurre normalizzazioni semantiche distruttive.

I limiti finali devono essere documentati e testati. Non devono essere scelti tanto bassi da rifiutare libri reali ragionevoli senza motivo.

## Sicurezza dei link — target M3.11

### Link interni

Devono restare confinati alla pubblicazione e risolversi attraverso i path virtuali OCF e le `ReadingLocation` già previste.

Un target assente o non risolvibile deve:

```text
non modificare ReadingLocation
non alterare il back-stack
non provocare accesso filesystem arbitrario
produrre diagnostica
```

### Link esterni

EReader non è un browser e non verifica automaticamente gli URL via rete.

Il contratto corrente M3.5 permette l'handoff esplicito per:

```text
http:
https:
mailto:
```

Le milestone di security hardening devono mantenere una allow-list esplicita e rifiutare schemi non previsti. In particolare contenuto EPUB non deve poter avviare implicitamente `file:`, `javascript:`, shell, interpreter o comandi locali.

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
