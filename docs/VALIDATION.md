# Validation — M3.5 Hotfix 1 — XHTML Smoke Fixture Alignment

M3.5 deve essere costruita esclusivamente dalla baseline validata M3.4 Hotfix 1.

## Gate

Windows:

```bat
.\validate.cmd
```

Linux/macOS:

```sh
./validate.sh
```

Esito atteso:

```text
M3.5 HOTFIX 1 VALIDATION PASSED
```

## Criteri M3.5 Hotfix 1

- il fixture `m3.5-link-smoke.epub` deve superare il parser Navigation sicuro senza DTD/DOCTYPE;
- `mimetype` del fixture resta prima entry e stored;
- nessuna modifica produttiva rispetto alla candidate M3.5 originale;

- `CliEntryPoint.Milestone == "M3.5"`;
- build Release con warnings-as-errors;
- suite completa: 454 casi attesi;
- indice hyperlink pre-layout su offset UTF-16;
- internal link -> ReadingLocation target + back stack bounded;
- Backspace non è persistito/configurato;
- external link boundary limitato a http/https/mailto;
- nessun network fetch/browser embedded;
- image preview M3.4 resta disponibile come fallback di Enter;
- state schema 3 e config schema 1 invariati;
- 11 step di gate, incluso `m3.5-link-smoke.epub` in `--plain`;
- smoke CLI consolidati passano.

Il gate automatico non apre browser/applicazioni esterne. L'apertura di un link `https` o `mailto` va verificata manualmente se desiderato.
