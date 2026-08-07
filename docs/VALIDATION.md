# Validation — M2.2 Metadata View

## Stato della catena

Ultima baseline autoritativa validata: **M2.1 — Interactive TOC** (`M2.1 VALIDATION PASSED`).

Candidate corrente: **M2.2 — Metadata View**.

M2.2 è costruita esclusivamente sopra la baseline M2.1 validata.

## Gate

Da una estrazione pulita:

```bat
validate.cmd
```

oppure:

```sh
./validate.sh
```

Il gate esegue:

1. restore;
2. build Release con analyzer e warnings-as-errors;
3. suite completa;
4. smoke `--help`;
5. smoke `--version`;
6. smoke `--foundation-info`;
7. lettura end-to-end stateless di `test-books/m1.0-smoke.epub` con `--plain`.

Output finale atteso:

```text
M2.2 VALIDATION PASSED
```

## Conteggio statico

- **344** `[Fact]`;
- **4** `[Theory]`;
- **16** casi `[InlineData]`;
- **360 casi attesi**.

## Criteri M2.2

Il PASS deve confermare, oltre a tutte le regressioni precedenti:

- M2.1 TOC resta funzionante e validato;
- `m` apre/chiude la vista metadata;
- `Esc` chiude metadata/TOC/help prima di uscire;
- apertura/scorrimento/chiusura metadata non modifica la `ReadingLocation`;
- `↑/↓` e `j/k` scorrono i metadata una riga;
- `PgUp/PgDn` scorrono i metadata di una pagina;
- metadata opzionali mancanti vengono omessi;
- contributor e relativi ruoli Domain vengono proiettati senza semantica EPUB;
- identificatori conservano l'eventuale schema Domain;
- descrizioni e valori lunghi vengono wrappati secondo la larghezza terminale;
- Unicode wide/emoji non oltrepassano la larghezza in celle prevista dal formatter;
- resize riformatta la vista metadata e clampa l'offset di scroll;
- `ReaderWindow` e `ReaderMetadataFormatter` non conoscono tipi EPUB/OPF;
- nessuno stato della vista metadata entra in `state.json`;
- `--plain` resta stateless.

## Evidenza precedente

- M0.1 Hotfix 2 — VALIDATED;
- M0.2 — VALIDATED;
- M0.3 Hotfix 1 — VALIDATED;
- M0.4 — VALIDATED;
- M0.5→M1.0 — VALIDATED;
- M1.1 — VALIDATED, 286/286;
- M1.2 Hotfix 1 — VALIDATED;
- M1.3 Hotfix 1 — VALIDATED;
- M1.4 Hotfix 2 — VALIDATED;
- M2.0 — VALIDATED;
- M2.0 Hotfix 1 — VALIDATED;
- M2.0 Hotfix 2 — VALIDATED;
- M2.1 — `M2.1 VALIDATION PASSED`.
