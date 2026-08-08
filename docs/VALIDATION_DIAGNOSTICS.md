# M0.7 — Validation & Diagnostics

## Obiettivo

M0.7 introduce un confine unico e non-throwing per gli errori **attesi** durante l'ingestione EPUB. Il codice chiamante non deve conoscere le eccezioni specifiche di Container, Package, Navigation o Content per distinguere un EPUB valido, malformato o non supportato.

La facade è:

```text
EpubPublicationValidator
        ↓
EpubValidationResult
├── Status = Valid | Invalid | Unsupported
├── Book?                         format-neutral Domain
├── Protection?                   metadata di protezione ispezionata
└── Diagnostics[]                 codici stabili + messaggio
```

Gli errori di programmazione e i guasti runtime non riconducibili al formato EPUB non vengono inghiottiti.

## Stati

### Valid

`Book` è non-null e pronto per il livello Application. Dalla M3.10 e M3.11 `Valid` non significa necessariamente “nessuna diagnostica”: il Container, la protection inspection, l'OPF e almeno il reading order primario devono essere affidabili, mentre navigation e risorse non essenziali possono essere degradate in modo deterministico con warning/recoverable diagnostics.

### Invalid

La pubblicazione viola un requisito necessario alla lettura affidabile, ad esempio ZIP/OCF non valido, OPF malformato, reading order primario assente/illeggibile, `encryption.xml` malformato o un failure del boundary di sicurezza che non può essere isolato. Un TOC non utilizzabile, da solo, non rende più `Invalid` un libro il cui contenuto primario è leggibile.

### Unsupported

La pubblicazione è riconoscibile ma richiede una feature deliberatamente fuori scope. In M0.7 il caso principale è la cifratura di risorse EPUB. EReader non prova a decrittare, derivare chiavi, interpretare licenze o aggirare DRM.

## Codici diagnostici

Le eccezioni delle milestone precedenti conservano i rispettivi enum numerici. M0.7 li proietta in codici testabili:

```text
ER-EPUB-CONTAINER-000...
ER-EPUB-PROTECTION-000...
ER-EPUB-PACKAGE-000...
ER-EPUB-NAVIGATION-000...
ER-EPUB-CONTENT-000...
```

I codici M0.7 non derivati direttamente da enum sono:

```text
ER-EPUB-PROTECTION-INFO-001         rights.xml presente
ER-EPUB-PROTECTION-INFO-002         font obfuscation riconosciuta presente
ER-EPUB-PROTECTION-UNSUPPORTED-001  risorse realmente cifrate
```

Il testo umano può essere migliorato in futuro senza cambiare il significato machine-readable del codice.

## META-INF/encryption.xml

OCF prevede `META-INF/encryption.xml` quando vengono cifrate risorse del container. I riferimenti presenti nei file `META-INF` sono risolti rispetto alla **root del container**, non rispetto alla cartella `META-INF`.

EReader esegue solamente ispezione bounded:

- massimo 1 MiB per `encryption.xml`;
- massimo 10.000 `EncryptedData`;
- DTD proibiti;
- `XmlResolver = null`;
- nessun accesso di rete;
- nessun uso di API crittografiche;
- `CipherReference` risolti tramite `OcfPath`;
- traversal e separator percent-encoded rifiutati;
- target inesistenti rifiutati;
- target duplicati rifiutati;
- file OCF che lo standard vieta di cifrare rifiutati, compreso il Package Document.

## Font obfuscation non è DRM

EReader riconosce come `FontObfuscation` due meccanismi usati negli EPUB:

```text
http://www.idpf.org/2008/embedding        standard EPUB/IDPF
http://ns.adobe.com/pdf/enc#RC            legacy Adobe/InDesign
```

Non vengono classificati come contenuto DRM/cifrato. Il secondo è mantenuto per compatibilità con EPUB legacy, in particolare EPUB 2 prodotti da tool storici. In un reader CLI il font incorporato non è necessario per produrre il testo, quindi la pubblicazione può restare `Valid`.

Il target deve comunque:

- esistere nel container;
- essere dichiarato nel manifest OPF;
- avere un media type font riconoscibile.

EReader non implementa de-offuscamento del font in M0.7 perché non serve alla pipeline testuale.

## Cifratura reale

Qualunque `EncryptedData` che non usa uno dei due algoritmi di font obfuscation riconosciuti viene classificato conservativamente come `UnsupportedEncryption`, anche quando `EncryptionMethod/@Algorithm` è assente.

Il validator interrompe la pipeline **prima** di interpretare il contenuto cifrato e restituisce:

```text
Status = Unsupported
Book = null
```

Questo mantiene il vincolo progettuale "EPUB senza DRM" e impedisce che la pipeline tenti accidentalmente di elaborare bytes cifrati come XHTML o immagini.

## rights.xml

La presenza di `META-INF/rights.xml` è registrata come informazione, non come prova automatica di DRM. OCF riserva il file per informazioni di rights management, ma la sua sola presenza non implica che le publication resources siano cifrate.

## Consumo in M1.0

M1.0 non ricostruisce la pipeline EPUB. Parte direttamente da:

```text
EpubPublicationValidator.Validate(...)
    ↓ Valid
result.Book
    ↓
BookConsoleRenderer / primo reader end-to-end
```

La CLI M1.0 scrive `Diagnostics` su stderr senza dipendere dagli enum e dalle eccezioni interne dell'adapter EPUB. La futura TUI potrà riusare lo stesso contratto.

## Riferimenti tecnici

- W3C EPUB 3.3 — Open Container Format, `META-INF/encryption.xml`, URL nei file `META-INF` e font obfuscation: <https://www.w3.org/TR/epub-33/>
- Adobe/IDPF technical note — Simple content protection for embedded fonts, algoritmo legacy `http://ns.adobe.com/pdf/enc#RC`.

Questi riferimenti descrivono il formato da riconoscere; non costituiscono una decisione di implementare decrittazione o DRM support.

## Evoluzione M3.8 e roadmap successiva

M0.7 resta il contratto autoritativo dell'**ingestione** (`Valid`, `Invalid`, `Unsupported`). M3.8 ha introdotto la tassonomia reader-wide format-neutral senza rompere questo contratto; M3.9 Hotfix 1 ha validato i guardrail di sicurezza dell'input. M3.10 Hotfix 2 ha validato il recovery dei failure non essenziali; M3.11 candidate aggiunge link integrity granulare; M3.12–M3.13 completeranno crash containment e corpus di affidabilità.

La nuova documentazione distingue esplicitamente:

- esito di ingestione EPUB;
- severità della diagnostica;
- recovery della risorsa/documento;
- `FatalDocumentError`, cioè documento irrecuperabile ma non guasto interno;
- `InternalError`, che non deve essere mascherato come EPUB non valido;
- `ReaderOperationStatus` con `Success`, `SuccessWithDiagnostics`, `DocumentUnreadable`, `InternalFailure`;
- bridge adapter-specific → reader-wide confinato al composition root CLI.

Riferimenti:

- [`DIAGNOSTICS.md`](DIAGNOSTICS.md)
- [`EPUB_FAILURE_MODEL.md`](EPUB_FAILURE_MODEL.md)
- [`EPUB_SECURITY_MODEL.md`](EPUB_SECURITY_MODEL.md)
- [`EPUB_RECOVERY_POLICY.md`](EPUB_RECOVERY_POLICY.md)
- [`EPUB_COMPATIBILITY.md`](EPUB_COMPATIBILITY.md)


## Codici M3.11

```text
ER-EPUB-RECOVERY-LINK-001
ER-EPUB-SECURITY-LINK-001
ER-EPUB-RECOVERY-NAVIGATION-003
```

Un link rotto o un target TOC rotto non viene riclassificato come `Invalid` quando può essere isolato senza perdere il reading order.
