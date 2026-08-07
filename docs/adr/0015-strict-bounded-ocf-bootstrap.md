# ADR-0015 — Bootstrap OCF stretto, bounded e con diagnostica strutturata

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Prima di leggere OPF o XHTML il reader deve stabilire che l'input sia un contenitore EPUB interpretabile. Il bootstrap processa dati non fidati: header ZIP, `mimetype` e XML di `container.xml`.

## Decisione

M0.3 applica un bootstrap deterministico e limitato:

1. legge il primo local ZIP header senza estrarre file;
2. richiede `mimetype` come prima entry fisica;
3. richiede `mimetype` stored/non compresso, senza extra field e con contenuto ASCII esatto `application/epub+zip`;
4. apre `ZipArchive` in sola lettura;
5. indicizza le entry con confronto ordinal case-sensitive e rifiuta duplicati;
6. limita il numero di entry a 100000;
7. legge `META-INF/container.xml` con DTD proibiti e resolver XML disabilitato;
8. limita `container.xml` a 1 MiB;
9. richiede container namespace corretto, `version="1.0"`, almeno un `rootfile` e media type `application/oebps-package+xml`;
10. limita i rootfile a 128 e verifica che ogni package document dichiarato esista;
11. usa il primo rootfile come rendition di default.

Gli errori di contenuto sono rappresentati da `EpubContainerException` + `EpubContainerErrorCode`.

## Conseguenze

- diagnostica testabile senza dipendere dal testo del messaggio;
- difese basilari contro input ostili o accidentalmente enormi;
- XXE/DTD non entrano nella pipeline;
- M0.4 può assumere che il package document di default sia localizzato e accessibile.

## Limiti intenzionali

M0.3 non è ancora un sostituto di EPUBCheck e non valida ogni campo di ogni local ZIP header. La conformance diagnostica più ampia resta M0.7.
