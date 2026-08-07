# M2.4 Hotfix 2 — Colori semantici della TUI

## Obiettivo

La TUI distingue la struttura editoriale senza modificare il testo logico o le coordinate di lettura.

| Elemento | Resa Terminal.Gui |
|---|---|
| Titolo/heading | cyan (azzurro) |
| `StrongSpan` | verde + bold |
| `EmphasisSpan` | giallo + italic |
| Strong + emphasis annidati | verde + bold + italic |
| Testo ordinario / overlay | bianco |
| Cornice finestra / separatori | grigio |
| Sfondo | nero |

La disponibilità effettiva degli attributi bold/italic dipende dall'emulatore terminale. Il colore rimane comunque il segnale primario.

## Boundary architetturale

`EbookReader.Layout` non conosce colori Terminal.Gui. Conserva soltanto:

```text
VisualLine
  ├── Kind = Heading / Body / ...
  └── StyleSpans[]
       ├── Strong
       └── Emphasis
```

La TUI converte questi ruoli in `Terminal.Gui.Drawing.Attribute` dentro `ReaderColorPalette` e li disegna con `ReaderBodyView`.

## Wrapping e Unicode

Gli style span sono espressi in indici UTF-16 locali alla riga visuale e vengono prodotti nello stesso passaggio che effettua il wrapping. Di conseguenza:

- un bold spezzato su più righe rimane bold su ciascuna riga;
- i prefissi `> ` delle citazioni e i marker delle liste rimangono testo normale;
- un grapheme/emoji non viene spezzato per applicare lo stile;
- la `ReadingLocation` continua a usare gli offset logici sorgente e non cambia.

## Cornice

La `Window` usa uno schema grigio; header, footer e body sovrascrivono esplicitamente lo schema con testo bianco. I due separatori introdotti in M2.0 Hotfix 2 usano lo stesso grigio della cornice.

Con Terminal.Gui 2.4.17 gli schemi custom vengono applicati tramite `View.SetScheme(Scheme?)`; non viene usata una proprietà `Scheme` assegnabile.
