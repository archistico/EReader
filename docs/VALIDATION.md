# Validation — M3.3 Hotfix 1 — Foundation Smoke Alignment

## Baseline e stacked chain

- M3.1 Hotfix 1 — **VALIDATED** (`M3.1 HOTFIX 1 VALIDATION PASSED`).
- M3.2 Themes — **CANDIDATE non ancora validata**.
- M3.3 è costruita esclusivamente sopra M3.2 e resta **STACKED CANDIDATE**.
- M3.3 Hotfix 1 è costruita esclusivamente sopra M3.3 e corregge il solo smoke test milestone rimasto a `M3.2`.

## Gate

Da estrazione pulita:

```bat
.\validate.cmd
```

oppure:

```bash
./validate.sh
```

Esito atteso:

```text
M3.2+M3.3 HOTFIX 1 STACKED VALIDATION PASSED
```

## Criterio Hotfix 1

- `FoundationSmokeTests` deve verificare `M3.3`, coerentemente con `CliEntryPoint.Milestone`;
- nessun file produttivo sotto `src/` deve differire dalla candidate M3.3 precedente.

## Criteri M3.2 + M3.3

- i tre temi M3.2 compilano e passano i regression test;
- `config.json` è separato da `state.json` schema 3;
- file config assente = preferenze default;
- schema config diverso da 1 viene rifiutato;
- tema sconosciuto viene rifiutato;
- keymap parziale eredita i default;
- collisioni e binding multi-grapheme vengono rifiutati;
- `ReaderWindow` usa i binding configurati per i tasti stampabili;
- frecce/PgUp/PgDn/Space/Tab/Enter/Esc/F1 restano disponibili;
- cambio tema viene riportato dalla TUI e salvato nel file preferenze;
- `--init-config` crea un file default bounded e `--config-path` espone il percorso;
- `EREADER_CONFIG_FILE` consente uno smoke isolato;
- ricerca, bookmark, progress, resize, TOC, metadata, library e library search non regrediscono.

## Prova manuale suggerita

1. `ereader --init-config`;
2. `ereader --config-path`;
3. modificare ad esempio `NextLine` da `j` a `x` e `PreviousLine` da `k` a `z`;
4. aprire un EPUB e verificare frecce + `x/z`;
5. premere il binding `CycleTheme`, uscire e riaprire il libro;
6. verificare che il tema scelto sia stato ripristinato;
7. verificare che `state.json` non contenga `theme` o `keymap`.

## Conteggio statico candidate

- 420 `[Fact]`;
- 4 `[Theory]`;
- 16 `[InlineData]`;
- **436 casi attesi**.
