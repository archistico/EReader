# Project Handoff — EReader M3.13 Hotfix 4 Architecture Contract Alignment Candidate

> **Baseline validata:** M3.13 Hotfix 1 (`M3.13 HOTFIX 1 VALIDATION PASSED`, 08/08/2026).
> **Candidate corrente:** M3.13 Hotfix 4 — architecture contract alignment sopra branding e status bar TUI responsive su due righe.

## Baseline e catena

- baseline autoritativa validata: **M3.13 Hotfix 1**;
- M3.12 Crash Containment: validata cumulativamente dal gate M3.13 Hotfix 1;
- M3.13 Reliability Corpus: validata cumulativamente dal gate M3.13 Hotfix 1;
- candidate corrente: **M3.13 Hotfix 4 — Architecture Contract Alignment**;
- gate candidate: `M3.13 HOTFIX 4 VALIDATION PASSED`.
- Hotfix 3 ha corretto esclusivamente l'integrazione di compilazione (`RepositoryRoot.Find()` e tuple footer TUI tramite `var footer`).
- Hotfix 4 riallinea due contratti architetturali storici al footer su due righe: il body usa `Dim.Fill(3)` / separatore `Pos.AnchorEnd(3)` e i due footer label `_footerPrimary` / `_footerSecondary` usano entrambi `PlainScheme`. Nessuna modifica al runtime rispetto a Hotfix 3.

Le Hotfix 2–4 non modificano parser, recovery, security, diagnostica, persistence o corpus EPUB.

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

La successiva validazione `M3.13 HOTFIX 1 VALIDATION PASSED` ha promosso cumulativamente M3.12 e M3.13; M3.13 Hotfix 1 è ora la baseline autoritativa.

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
506 Fact
10 Theory
57 InlineData
563 casi eseguibili attesi
16 step
20 EPUB nel corpus reliability
```

Eseguire da estrazione pulita:

```bat
.\validate.cmd
```

## Prossimo punto dopo il gate

Con M3.8–M3.13 chiusi, la roadmap torna alle feature: **M4.0 — Managed Library**.


## M3.13 Hotfix 4 — Architecture Contract Alignment

- preserva branding TUI/CLI `EReader by Emilie Rollandin`;
- preserva footer reader e libreria su due righe e modalità compatta automatica;
- aggiorna i vecchi architecture contract M2.0/M2.4 perché verifichino la nuova struttura a due label anziché il footer singolo rimosso;
- nessuna modifica a `src/` rispetto a Hotfix 3, né a Domain/Application/EPUB/Layout, state schema o config schema.
