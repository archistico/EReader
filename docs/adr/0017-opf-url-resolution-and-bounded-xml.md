# ADR-0017 — Risoluzione URL OPF sul filesystem OCF e XML bounded

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

Gli `href` del manifest sono URL relativi al Package Document o URL assoluti. Una conversione ingenua a path Windows/Linux introdurrebbe differenze di piattaforma e rischi di traversal. Inoltre un Package Document è input non fidato.

## Decisione

- gli href locali vengono risolti rispetto alla directory virtuale dell'OPF e normalizzati tramite `OcfPath`;
- il lookup resta ordinal case-sensitive;
- traversal sopra la root, separator percent-encoded, `file:` URI e self-reference del package sono rifiutati;
- le risorse locali dichiarate devono esistere nel contenitore;
- URL assoluti non-file vengono preservati come `Uri` senza retrieval in M0.4;
- il Package Document è limitato a 4 MiB, con DTD proibiti e `XmlResolver = null`.

## Conseguenze

- nessun path host entra nel parsing OPF;
- gli stessi EPUB producono la stessa risoluzione su Windows/Linux/macOS;
- M0.4 non effettua rete;
- gli errori sono categorizzati tramite `EpubPackageErrorCode`.

## Alternative considerate

- `Path.Combine`: scartato perché il namespace è OCF/URL, non filesystem host;
- estrarre prima lo ZIP: scartato da ADR-0014;
- seguire automaticamente URL remoti: scartato per sicurezza, determinismo e scope.
