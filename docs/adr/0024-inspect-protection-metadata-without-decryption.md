# ADR-0024 — Ispezionare la protezione EPUB senza implementare decrittazione

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

OCF permette `META-INF/encryption.xml` per descrivere risorse cifrate. Lo stesso file identifica anche i font offuscati con l'algoritmo EPUB standard, che lo standard distingue dalla cifratura vera e propria.

EReader ha scope solo EPUB non-DRM e non necessita dei font incorporati per il rendering testuale CLI.

## Decisione

EReader analizza `encryption.xml` in modo bounded e senza API crittografiche.

- `http://www.idpf.org/2008/embedding` viene classificato come `FontObfuscation` standard;
- `http://ns.adobe.com/pdf/enc#RC` viene classificato come `FontObfuscation` legacy per compatibilità con EPUB 2/tool Adobe storici;
- entrambi non bloccano il reader testuale purché il target sia un font valido del manifest;
- ogni altra forma di `EncryptedData` è `UnsupportedEncryption`;
- EReader non decritta, non deriva chiavi, non interpreta licenze DRM e non tenta circumvention;
- `rights.xml` è informativo e non prova da solo la presenza di DRM.

## Conseguenze

- EPUB con font obfuscation IDPF o Adobe legacy possono essere letti come testo;
- EPUB con contenuto realmente cifrato falliscono presto come `Unsupported`;
- bytes cifrati non raggiungono AngleSharp;
- il confine no-DRM è testabile automaticamente.

## Alternative considerate

- rifiutare qualunque `encryption.xml`: respinto perché confonderebbe font obfuscation e DRM;
- implementare font de-obfuscation: rinviato, inutile per la TUI testuale corrente;
- supportare algoritmi di cifratura/DRM: fuori scope.
