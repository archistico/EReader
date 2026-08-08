# M3.13 — Corrupted EPUB Corpus & Reliability Gate

## Scopo

M3.13 trasforma i contratti M3.8–M3.12 in un gate riproducibile basato su file EPUB reali. Il corpus vive in `test-books/reliability/` ed è intenzionalmente composto da documenti validi, degradati, corrotti e security-sensitive.

Il principio verificato resta: **un EPUB può essere illeggibile; EReader no**.

## Catena di validazione

M3.13 e la M3.12 inclusa nella sua catena sono state validate cumulativamente l'08/08/2026 con `M3.13 HOTFIX 1 VALIDATION PASSED`. M3.13 Hotfix 1 è quindi la baseline autoritativa corrente per il corpus reliability.

## Corpus

Il corpus contiene 20 file `.epub`. Gli outcome attesi sono dichiarati in `test-books/reliability/README.md` e verificati da `ReliabilityCorpusTests`.

Classi coperte:

- controllo EPUB minimale valido;
- navigation mancante recuperabile;
- immagine referenziata mancante;
- anchor interno rotto;
- schema esterno non consentito;
- `container.xml` mancante/malformato;
- rootfile OPF mancante;
- OPF XML malformato;
- primary spine content mancante;
- XHTML con carattere XML illegale;
- UTF-8 invalido;
- encryption non supportata;
- duplicate ZIP entry;
- traversal e drive-prefixed entry;
- Unix symlink/special entry;
- compression ratio patologico;
- entry con dimensione decompressa dichiarata oltre 256 MiB;
- ZIP troncato.

## Outcome

Il validator deve produrre soltanto outcome deterministici già definiti:

- `Valid` senza diagnostiche per il controllo leggibile;
- `Valid` con diagnostica per recovery/warning;
- `Invalid` per documento strutturalmente non affidabile;
- `Unsupported` per encryption deliberatamente fuori scope.

Il livello CLI deve tradurli in:

- successo senza marker;
- `READABLE_DEGRADED`;
- `READABLE_WITH_WARNINGS`;
- `DOCUMENT_UNREADABLE`.

Un input del corpus non deve diventare `INTERNAL_FAILURE`: quel marker è riservato a failure interne inattese di EReader.

## Sicurezza e immutabilità

Le fixture security-sensitive vengono validate direttamente dal file e il test confronta i byte prima/dopo. La validazione non deve modificare l'archivio sorgente. Restano inoltre autoritativi i contratti M3.9: nessuna estrazione generale, nessun fetch di rete e nessun path fuori dall'OCF virtuale.

## Limiti del corpus M3.13

Il corpus è sintetico e mirato alla reliability/security foundation. Non sostituisce M5.1, che dovrà aggiungere EPUB reali e una matrice di compatibilità molto più ampia.
