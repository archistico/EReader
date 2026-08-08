# Validation — M3.6 Footnotes / Endnotes UX

M3.6 deve essere costruita esclusivamente dalla baseline validata M3.5 Hotfix 1.

## Gate

```bat
.\validate.cmd
```

oppure:

```sh
./validate.sh
```

Esito atteso:

```text
M3.6 VALIDATION PASSED
```

## Criteri M3.6

- restore e build Release senza warning/errori;
- suite completa xUnit/MTP;
- `CliEntryPoint.Milestone == "M3.6"`;
- `HyperlinkRole.NoteReference` resta format-neutral nel Domain;
- `epub:type="noteref"` viene interpretato solo dall'adapter EPUB;
- `BookHyperlinkIndex` conserva ruolo e range UTF-16;
- note interne usano lo stesso stack Backspace bounded M3.5;
- `state.json` schema 3 e `config.json` schema 1 invariati;
- smoke EPUB M3.6 passa in `--plain` senza avviare browser/viewer.


Audit statico M3.6: **444 Fact + 4 Theory + 16 InlineData = 460 casi attesi**.
