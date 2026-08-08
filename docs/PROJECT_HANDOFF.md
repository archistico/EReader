# Project Handoff — EReader M3.13 Corrupted EPUB Corpus & Reliability Gate Candidate

> **Hotfix 1 corrente:** allineamento analyzer `CA2201` nel solo test `ProcessCriticalFailureIsNotContainedAsOrdinaryInternalFailure`; runtime e corpus M3.13 restano invariati.

## Baseline e catena

- ultima baseline autoritativa validata: **M3.11 Hotfix 1**;
- gate baseline: `M3.11 HOTFIX 1 VALIDATION PASSED` (08/08/2026);
- dipendenza stacked non ancora validata: **M3.12 — Crash Containment & Diagnostics UX**;
- candidate corrente: **M3.13 — Corrupted EPUB Corpus & Reliability Gate**;
- gate cumulativo: `M3.13 HOTFIX 1 VALIDATION PASSED`.

La candidate M3.13 include integralmente M3.12. Non promuovere M3.12 o M3.13 a baseline autoritativa finché il gate locale non passa.

## Obiettivo M3.13

Rendere riproducibili i contratti reliability/security M3.8–M3.12 usando file EPUB reali su disco e verificare che document failure previsti restino distinti dagli errori interni EReader.

## Revisione documentazione e tooling release

Dopo la preparazione della candidate M3.13 è stata applicata una revisione **docs/tooling-only** senza modifiche a `src/`, `tests/` o `test-books/`:

- README radice riscritto come guida semplice per installazione e uso;
- nuovo indice `docs/README.md`;
- nuovi script `clean.cmd|sh`;
- nuovi script `publish-release.cmd|sh`, con gate obbligatorio e output self-contained `win-x64`/`linux-x64`;
- nuovo `docs/RELEASE.md`;
- eliminati documenti transitori/duplicati: `FIRST_READABLE_EPUB.md`, `READER_COLORS.md`, `VALIDATION_DIAGNOSTICS.md` e il vecchio report `docs/analysis/M1.1_GRAPHIFY_REPORT.md`.

Questa manutenzione non promuove M3.12 o M3.13: l'ultima baseline autoritativa validata resta M3.11 Hotfix 1 finché il gate cumulativo M3.13 non passa.

## Corpus

`test-books/reliability/` contiene 20 EPUB. `README.md` nella stessa cartella dichiara outcome e diagnostic code attesi. Il corpus copre validità, degraded reading, warning, corruption, unsupported encryption e input ZIP ostili/pathologici.

`oversized-entry-metadata.epub` è volutamente compatto: dichiara nel metadata ZIP una entry >256 MiB senza distribuire un payload enorme. `compression-ratio-bomb.epub` contiene invece 17 MiB logici altamente comprimibili ed è circa pochi KiB sul disco.

## Test principali

- `ReliabilityCorpusTests`: tutti i 20 file attraverso `EpubPublicationValidator`;
- security fixture: confronto byte-before/after per assicurare che il sorgente non venga mutato;
- `ReliabilityCorpusCliTests`: casi rappresentativi via `CliEntryPoint --plain`, con verifica che non appaia `INTERNAL_FAILURE`;
- test M3.12 crash containment restano integralmente inclusi.

## Invarianti

- M3.11 Hotfix 1 integralmente preservata;
- unico catch generale produttivo resta il boundary M3.12;
- parser/validator EPUB senza catch-all;
- nessun fetch automatico;
- nessuna estrazione generale dell'EPUB;
- nessuna modifica al file EPUB sorgente;
- state schema 4 e config schema 1 invariati;
- exit 3/4 = document failure, exit 6 = internal failure contenibile.

## Gate

```text
503 Fact
10 Theory
57 InlineData
560 casi eseguibili attesi
16 step
20 EPUB nel corpus reliability
```

Eseguire da estrazione pulita:

```bat
.\validate.cmd
```

## Prossimo punto dopo il gate

Con M3.8–M3.13 chiusi, la roadmap torna alle feature: **M4.0 — Managed Library**.
