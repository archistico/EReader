# ADR-0045 — La ricerca libreria è transiente e classificata nell'Application layer

- **Stato:** Accepted
- **Data:** 2026-08-08

## Contesto

M3.0 introduce una cronologia bounded di massimo 200 libri recenti. Con l'aumentare delle entry, la sola navigazione sequenziale diventa scomoda. Il filtro non deve però diventare stato persistente né introdurre dipendenze Terminal.Gui nel modello della libreria.

## Decisione

M3.1 introduce `ReadingHistorySearch` in `EbookReader.Application.Library`. La ricerca opera esclusivamente su `ReadingHistoryEntry` e considera titolo, autore, nome file e path. Il matching è case-insensitive e accent-insensitive. Titolo, autore e nome file supportano anche sottosequenze fuzzy; il path completo è limitato a exact/prefix/substring per evitare falsi positivi dovuti a caratteri sparsi tra directory e nome file. I risultati vengono classificati dando priorità ai match di titolo, poi autore, nome file e path, con l'ordine cronologico originale come tie-break deterministico.

La query è transiente, limitata a 128 code unit UTF-16 e non viene serializzata in `state.json`. `LibraryWindow` gestisce esclusivamente input e presentazione e delega il ranking all'Application layer.

## Conseguenze

- il filtro resta indipendente da Terminal.Gui e dal layout;
- nessuna migrazione dello schema JSON è necessaria;
- la cronologia continua a essere l'unica fonte persistente della libreria;
- risultati e ranking sono deterministici;
- il costo rimane bounded dal limite di 200 entry M3.0.

## Alternative considerate

- semplice `Contains`: meno utile con titoli/path parziali e refusi di omissione;
- fuzzy search dentro `LibraryWindow`: avrebbe accoppiato policy di ricerca e TUI;
- persistere l'ultima query: stato UI effimero senza valore per il resume;
- dipendenza da una libreria fuzzy esterna: non necessaria per un insieme bounded di 200 entry.
