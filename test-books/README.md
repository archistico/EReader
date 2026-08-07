# Test books

Questa directory contiene esclusivamente pubblicazioni sintetiche costruite per i gate automatici di EReader.

## `m1.0-smoke.epub`

EPUB 3 minimale, reflowable e senza DRM. Viene eseguito da `validate.cmd` / `validate.sh` per verificare realmente la pipeline completa:

```text
OCF → OPF → Navigation → XHTML → Domain → CLI
```

Il contenuto è originale e creato per il progetto; non è un ebook di terze parti.
