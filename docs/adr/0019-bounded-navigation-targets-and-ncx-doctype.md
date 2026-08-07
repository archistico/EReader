# ADR-0019 — Target di navigazione OCF bounded e gestione sicura del DOCTYPE NCX

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

I riferimenti di `nav.xhtml` e NCX sono URL relativi al documento di navigazione e possono includere fragment. Gli NCX EPUB 2 storici contengono frequentemente il DOCTYPE canonico NISO/DAISY. Risolvere DTD o entità esterne sarebbe però incompatibile con il modello di sicurezza del parser.

## Decisione

- i riferimenti locali vengono separati in resource path + fragment;
- il resource path viene normalizzato tramite `OcfPath`, mai tramite `System.IO.Path`;
- percent-escape invalidi, traversal, separator encoded, query locali e URL assoluti nei navigation aid supportati sono rifiutati;
- i target EPUB 3 di `toc`, `page-list` e `landmarks` devono risolvere a risorse top-level ottenute dallo spine/fallback chain;
- i target NCX devono risolvere a top-level Content Document derivati dallo spine o dalla relativa fallback chain;
- l'esistenza dell'elemento identificato dal fragment non viene ancora verificata: sarà possibile quando M0.6 interpreterà i Content Document;
- `nav.xhtml` usa XML con DTD proibiti;
- NCX ammette soltanto il DOCTYPE canonico NISO/DAISY 2005-1, rifiuta internal subset e mantiene `XmlResolver = null`, quindi la DTD esterna non viene recuperata; l'espansione di entità resta comunque bounded come difesa aggiuntiva;
- se il DOCTYPE canonico è presente, `playOrder` è obbligatorio sui `navPoint`; senza DOCTYPE può essere omesso secondo l'eccezione EPUB 2.0.1;
- documenti e gerarchie sono bounded per byte, numero nodi e profondità.

## Conseguenze

- path e fragment sono deterministici e indipendenti dal filesystem host;
- gli NCX reali con DOCTYPE canonico restano leggibili senza rete e ne vengono rispettati i requisiti `playOrder` per i navPoint;
- non vengono introdotti XXE o fetch esterni;
- la validazione dell'anchor concreto resta separata dal parsing strutturale della navigazione.

## Alternative considerate

### Proibire sempre il DOCTYPE NCX

Scartata perché renderebbe il reader inutilmente incompatibile con molti EPUB 2 storici conformi.

### Risolvere la DTD canonica

Scartata: introdurrebbe I/O esterno e una superficie di sicurezza non necessaria.
