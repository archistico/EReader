# Test books

Questa directory contiene esclusivamente pubblicazioni sintetiche costruite per i gate automatici di EReader.

## `m1.0-smoke.epub`

EPUB 3 minimale, reflowable e senza DRM. Viene eseguito da `validate.cmd` / `validate.sh` per verificare realmente la pipeline completa:

```text
OCF → OPF → Navigation → XHTML → Domain → CLI
```

Il contenuto è originale e creato per il progetto; non è un ebook di terze parti.


## m3.4-image-smoke.epub

EPUB 3 minimale aggiunto in M3.4. Contiene un vero PNG 1×1 locale referenziato da `<img alt="Pixel PNG di prova">`. Il validation gate lo apre in `--plain` per verificare ingestione/placeholder senza avviare viewer esterni. Per la prova manuale TUI, aprire il file normalmente, navigare sul placeholder e premere `Enter`.

## m3.5-link-smoke.epub

EPUB 3 minimale per M3.5. Contiene un link interno `#target` e un link esterno `https://example.com/`. Il gate automatico lo apre solo con `--plain`, quindi non avvia browser/applicazioni esterne. Per la prova manuale TUI, portarsi sulla riga “destinazione interna”, premere `Enter`, verificare il salto, poi `Backspace`; sul link Example, `Enter` deve delegare l'URL al browser di sistema.
