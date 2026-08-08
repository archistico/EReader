# Validation — M3.13 Hotfix 4 Architecture Contract Alignment

**Baseline autoritativa:** M3.13 Hotfix 1, validata l'08/08/2026 con gate `M3.13 HOTFIX 1 VALIDATION PASSED`. Il gate cumulativo ha validato anche M3.12 Crash Containment e M3.13 Reliability Corpus.

**Candidate corrente:** M3.13 Hotfix 4. Mantiene integralmente il runtime della Hotfix 3 e riallinea due architecture contract storici che attendevano ancora il footer singolo M2.x. I test ora verificano il body con `Dim.Fill(3)`, il separatore a `Pos.AnchorEnd(3)` e i due label `_footerPrimary` / `_footerSecondary` con `PlainScheme`. Nessuna modifica ai sorgenti produttivi rispetto a Hotfix 3.

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
M3.13 HOTFIX 4 VALIDATION PASSED
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
506 Fact
10 Theory
57 InlineData
563 casi eseguibili attesi (Fact + InlineData)
```

Il conteggio autoritativo resta quello prodotto dal gate locale.


## Tooling di manutenzione e release

La revisione documentale/tooling successiva alla preparazione M3.13 non modifica `src/`, `tests/` o il corpus. Aggiunge:

- `clean.cmd` / `clean.sh` per rimuovere ricorsivamente `bin` e `obj`;
- `publish-release.cmd` / `publish-release.sh` per produrre asset self-contained GitHub-ready **solo dopo il superamento di questo gate**;
- `docs/RELEASE.md` e un README radice orientato all'utente.

Lo script release richiama il gate invece di duplicarne la logica.
