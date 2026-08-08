# Project Handoff — EReader M3.3 Hotfix 1

## Stato

- **Baseline autoritativa validata:** M3.1 Hotfix 1 — Library Search.
- Gate utente: `M3.1 HOTFIX 1 VALIDATION PASSED`.
- **M3.2 Themes:** CANDIDATE non ancora validata.
- **Candidate corrente:** M3.3 Hotfix 1 — Foundation smoke alignment, costruita sopra M3.3.
- Stato corrente: **STACKED CANDIDATE**.
- Hotfix 1 corregge esclusivamente il contratto `FoundationSmokeTests`: il prodotto dichiarava già `M3.3`, mentre il test attendeva ancora `M3.2`. Nessun file produttivo sotto `src/` viene modificato.
- Target: .NET 10 / C# 14 / Terminal.Gui 2.4.17.

## Catena candidate

```text
M3.1 Hotfix 1 VALIDATED
        ↓
M3.2 Themes CANDIDATE
        ↓
M3.3 Configurable Keymap & Preferences STACKED CANDIDATE
        ↓
M3.3 Hotfix 1 Foundation Smoke Alignment STACKED CANDIDATE
```

M3.2 non deve essere promossa implicitamente a baseline finché il gate locale non passa. La validazione di M3.3 certifica insieme la catena M3.2→M3.3.

## M3.3 Hotfix 1

Correzione strettamente di test/handoff: `FoundationSmokeTests.MilestoneIsM33()` verifica `CliEntryPoint.Milestone == "M3.3"`. Tutti i 435 altri casi del tentativo locale erano già passati.

## M3.3

Nuovo `config.json` indipendente da `state.json`:

- schema configurazione 1;
- tema: `semantic-dark`, `paper-light`, `monochrome`;
- keymap stampabile case-sensitive;
- binding singolo grapheme e collision-free;
- proprietà omesse = default;
- massimo 64 KiB;
- scrittura atomica same-directory;
- `EREADER_CONFIG_FILE` per override;
- `ereader --config-path`;
- `ereader --init-config`.

I tasti speciali frecce/PgUp/PgDn/Space/Tab/Enter/Esc/F1 restano sempre disponibili e non sono configurabili in M3.3.

Il cambio tema con `CycleTheme` viene restituito dalla TUI e salvato in `config.json` all'uscita. Nessuna preferenza UI entra in `ReadingLocation`, bookmark, cronologia o `state.json` schema 3.

## Invarianti

- Domain/Epub/Application/Layout non dipendono dalla configurazione TUI;
- `ReadingLocation` resta l'unica posizione durevole;
- pagina/riga/viewport/progresso non vengono persistiti come coordinate;
- M3.1 search policy resta invariata;
- M3.2 semantic roles restano nel Layout, colori concreti nel CLI/TUI;
- configurazione invalida produce warning e fallback ai default senza bloccare la lettura.

## Gate

```text
.\validate.cmd
```

Esito atteso:

```text
M3.2+M3.3 HOTFIX 1 STACKED VALIDATION PASSED
```

Conteggio statico: 420 Fact + 16 InlineData = **436 casi attesi** (4 Theory).
