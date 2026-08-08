# Validation — M3.13 Corrupted EPUB Corpus & Reliability Gate

> **Hotfix 1 — CA2201 analyzer alignment.** Il test di crash containment che verifica l'esclusione di `OutOfMemoryException` costruisce intenzionalmente l'eccezione riservata; la soppressione `CA2201` è confinata a quel solo metodo di test. Nessun file produttivo cambia rispetto alla candidate M3.13 Docs1.

M3.13 è una candidate **stacked sopra M3.12**, preparata mentre M3.12 non era ancora validata localmente. L'ultima baseline autoritativa validata resta **M3.11 Hotfix 1** (`M3.11 HOTFIX 1 VALIDATION PASSED`, 08/08/2026). Il gate M3.13 è cumulativo: se passa, valida anche la catena M3.12 inclusa nel pacchetto.

## Gate

Windows:

```bat
.\validate.cmd
```

Linux/macOS:

```sh
./validate.sh
```

Il gate esegue 16 step. La suite completa include il corpus M3.13 e lo step dedicato ne verifica anche l'inventory fisico di 20 EPUB.

Output finale atteso:

```text
M3.13 HOTFIX 1 VALIDATION PASSED
```

## Contratti M3.13

- `CliEntryPoint.Milestone == "M3.13"`;
- 20 EPUB sotto `test-books/reliability/`;
- ogni fixture ha outcome e diagnostic code documentati;
- `ReliabilityCorpusTests` attraversa tutti i file tramite `EpubPublicationValidator`;
- fixture security-sensitive non vengono mutate durante la validazione;
- `ReliabilityCorpusCliTests` attraversa casi rappresentativi via `--plain`;
- document failure previsti non diventano `[INTERNAL_FAILURE]`;
- M3.12 mantiene un solo catch-all al composition root;
- state schema 4, config schema 1 e `ReadingLocation` invariati;
- nessun fetch di rete e nessuna estrazione generale del contenitore.

## Corpus

Vedi `RELIABILITY_CORPUS.md` e `test-books/reliability/README.md`.

## Conteggio statico

```text
503 Fact
10 Theory
57 InlineData
560 casi eseguibili attesi (Fact + InlineData)
```

Il conteggio autoritativo resta quello prodotto dal gate locale.


## Tooling di manutenzione e release

La revisione documentale/tooling successiva alla preparazione M3.13 non modifica `src/`, `tests/` o il corpus. Aggiunge:

- `clean.cmd` / `clean.sh` per rimuovere ricorsivamente `bin` e `obj`;
- `publish-release.cmd` / `publish-release.sh` per produrre asset self-contained GitHub-ready **solo dopo il superamento di questo gate**;
- `docs/RELEASE.md` e un README radice orientato all'utente.

Lo script release richiama il gate invece di duplicarne la logica.
