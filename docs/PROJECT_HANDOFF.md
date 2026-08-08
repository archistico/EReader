# Project Handoff — EReader M3.7 Docs1 — Reliability & Security Roadmap

## Baseline autoritativa

- Baseline validata: `EReader_M3.7_Hotfix1_CompilationIntegration_NET10_Candidate.zip`.
- Gate: `M3.7 HOTFIX 1 VALIDATION PASSED`.
- Validazione utente: 08/08/2026.
- M3.7 include Highlights & Personal Notes con `state.json` schema 4.
- M3.7 Hotfix 1 corregge i tre difetti di integrazione/compilazione del primo candidato M3.7 senza cambiare il contratto funzionale.

## Revisione Docs1

Questa revisione è **documentazione-only**. Non modifica:

- file sotto `src/`;
- file sotto `tests/`;
- fixture EPUB;
- `Directory.Build.props` / versione assembly;
- `CliEntryPoint.Milestone`;
- `validate.cmd` / `validate.sh`;
- schema `state.json` o `config.json`;
- comportamento runtime.

Docs1 consolida lo stato validato e rende autoritativa la roadmap di robustezza da affrontare prima della libreria M4.0.

Documenti aggiunti:

- `docs/DIAGNOSTICS.md`;
- `docs/EPUB_FAILURE_MODEL.md`;
- `docs/EPUB_SECURITY_MODEL.md`;
- `docs/EPUB_RECOVERY_POLICY.md`;
- `docs/EPUB_COMPATIBILITY.md`.

Documenti aggiornati:

- `README.md`;
- `docs/ROADMAP.md`;
- `docs/ARCHITECTURE.md`;
- `docs/VALIDATION_DIAGNOSTICS.md`;
- `docs/PROJECT_HANDOFF.md`.

## Principio guida della prossima fase

> **Un EPUB può essere illeggibile. EReader no.**

Un errore recuperabile deve degradare soltanto la risorsa o parte del libro interessata. Un errore irreversibile del documento può impedire l'apertura di quel libro, ma EReader deve restare operativo, preservare lo stato valido precedente e spiegare chiaramente il motivo.

## Roadmap immediata

1. **M3.8 — Diagnostics Foundation & Failure Taxonomy**
2. **M3.9 — Defensive EPUB Loading & Input Security**
3. **M3.10 — EPUB Recovery & Degraded Reading**
4. **M3.11 — Link Integrity & Navigation Security**
5. **M3.12 — Crash Containment & Diagnostics UX**
6. **M3.13 — Corrupted EPUB Corpus & Reliability Gate**
7. **M4.0 — Managed Library**

M3.8–M3.13 hanno priorità su M4.0.

## Contratti da preservare

### Ingestione M0.7

```text
EpubPublicationValidator
  → Valid
  → Invalid
  → Unsupported
```

La nuova tassonomia non deve rompere questo contratto.

### Stato logico

- `ReadingLocation` resta l'unica coordinata autoritativa di lettura;
- nessuna pagina/riga/viewport persistita;
- bookmark, highlight e note restano book-scoped e logici;
- un'apertura fallita non deve sostituire uno stato valido precedente.

### Security boundary già presente

- nessuna estrazione generale dell'EPUB;
- path OCF normalizzati e traversal rifiutato;
- DTD/XXE disabilitati ai boundary XML;
- input Content bounded;
- nessuna rete automatica;
- nessun JavaScript;
- nessuna decrittazione/circumvention DRM;
- viewer/browser esterni solo dopo azione esplicita e secondo i contratti M3.4/M3.5.

## Target M3.8

M3.8 deve essere una milestone piccola e infrastrutturale. Deve definire un modello diagnostico uniforme, lasciando a M3.9–M3.13 i casi concreti di hardening e recovery.

Tassonomia documentale target:

```text
Info
Warning
RecoverableError
FatalDocumentError
InternalError
```

Esiti UX minimi target:

```text
SUCCESS
SUCCESS_WITH_DIAGNOSTICS
DOCUMENT_UNREADABLE
```

`FatalDocumentError` significa documento irrecuperabile; non autorizza il crash dell'applicazione. `InternalError` resta distinto dagli errori EPUB attesi per non nascondere bug.

## Validation baseline

Da estrazione pulita:

```bat
.\validate.cmd
```

Gate baseline già validato:

```text
M3.7 HOTFIX 1 VALIDATION PASSED
```

Audit statico baseline:

- 454 `[Fact]`;
- 4 `[Theory]`;
- 16 `[InlineData]`;
- 470 casi attesi;
- 51 ADR numerati;
- 12 step nel validation gate;
- nessun `bin/`, `obj/`, `graphify-out/` nel package sorgente pulito.

La Docs1 non richiede un nuovo gate funzionale perché non cambia codice o test; prima di usarla come base di sviluppo va comunque verificato che il package differisca dalla baseline soltanto nella documentazione dichiarata.
