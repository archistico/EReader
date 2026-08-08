# Project Handoff — EReader M3.7 Hotfix 1 — Compilation Integration

## Baseline

- Baseline autoritativa validata: `EReader_M3.6_Hotfix1_HelpContract_NET10_Candidate.zip`.
- Gate baseline: `M3.6 HOTFIX 1 VALIDATION PASSED`.
- Candidate M3.7 originale testata localmente: `EReader_M3.7_HighlightsPersonalNotes_NET10_Candidate.zip`; build fallita con 6 errori di integrazione/compilazione.
- Candidate corrente: `EReader_M3.7_Hotfix1_CompilationIntegration_NET10_Candidate.zip`.
- Hotfix 1 è costruita esclusivamente sopra la candidate M3.7 originale, a sua volta costruita sopra M3.6 Hotfix 1.

## M3.7 Hotfix 1

Correzioni strettamente di compilazione/integrazione:

- `M37AnnotationsStayLogicalAndOutsideLayoutAndConfiguration` riportato dentro `ArchitectureContractTests` invece che nella classe statica `RepositoryRoot`;
- aggiunto a `ReadingAnnotationTests` l'helper privato `TemporaryDirectory`, coerente con gli altri test di stato;
- aggiunto `using EbookReader.Domain.Content;` a `ReaderBodyView` per risolvere `BlockId`;
- nessuna modifica al modello persistente, ai comandi F2/F3/F4 o al comportamento runtime previsto da M3.7.

## Funzionalità M3.7

- `state.json` evolve a schema 4; schema 1/2/3 restano leggibili.
- F2: toggle highlight sulla riga logica corrente.
- F3: add/edit nota personale alla `ReadingLocation` corrente.
- F4: lista combinata annotazioni.
- Highlight persistiti come intervalli UTF-16 half-open nello stesso blocco Domain.
- Note persistite come `ReadingLocation + Text + UpdatedUtc`.
- Restore richiede path + BookId + location valida.
- `config.json` resta schema 1; F2-F4 sono special keys fissi.
- Layout/EPUB restano annotation-unaware.
- Rendering highlight line-level nel CLI/TUI; range persistito preciso.
- Limiti: 1.000 highlight complessivi / 250 per libro; 500 note / 100 per libro; 2.048 UTF-16 per nota; 131.072 UTF-16 testo note complessivo; state file max 1 MiB.

## Validation

Eseguire da estrazione pulita:

```bat
.\validate.cmd
```

Gate atteso:

```text
M3.7 HOTFIX 1 VALIDATION PASSED
```

Audit statico candidate:

- 454 `[Fact]`;
- 4 `[Theory]`;
- 16 `[InlineData]`;
- 470 casi attesi;
- 51 ADR numerati;
- nessun `bin/`, `obj/`, `graphify-out/` nel package.

M3.7 Hotfix 1 resta CANDIDATE fino al gate locale dell'utente.
