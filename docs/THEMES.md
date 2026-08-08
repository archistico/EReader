# M3.2 — Themes

M3.2 aggiunge temi transitori al reader fullscreen senza cambiare i ruoli semantici prodotti dal layout.

## Comando

Durante la lettura:

```text
c    tema successivo
```

Ordine ciclico:

```text
Semantico scuro → Carta chiara → Monocromatico → Semantico scuro
```

Il comando funziona anche mentre sono visibili help, indice, metadata o bookmark. Durante il prompt `/` la lettera `c` resta naturalmente testo della query.

## Ruoli semantici

Il layout continua a conoscere soltanto:

```text
VisualLineKind.Heading
VisualTextStyle.Strong
VisualTextStyle.Emphasis
VisualTextStyle.StrongEmphasis
```

La TUI li mappa tramite `ReaderTheme`.

### Semantico scuro

- testo: bianco su nero;
- heading: cyan su nero;
- strong: verde + Bold;
- emphasis: giallo + Italic;
- strong+emphasis: verde + Bold+Italic;
- cornice/separatori: grigio su nero.

### Carta chiara

- testo: nero su bianco;
- heading: cyan + Bold;
- strong: verde + Bold;
- emphasis: nero + Italic;
- strong+emphasis: verde + Bold+Italic;
- cornice/separatori: grigio su bianco.

### Monocromatico

- testo: bianco su nero;
- heading/strong: bianco + Bold;
- emphasis: bianco + Italic;
- strong+emphasis: bianco + Bold+Italic;
- cornice/separatori: grigio su nero.

## Persistenza

In M3.2 la scelta tema nasce transiente e non viene scritta in `state.json` schema 3. **M3.3 implementa il boundary previsto**: il tema viene persistito in `config.json` schema 1, sempre separato dallo stato dei libri. Vedi `CONFIGURATION_KEYMAP.md` e ADR-0047.

## Invarianti

- nessun colore o nome tema in `EbookReader.Layout`;
- nessun tema in Domain/EPUB/Application State;
- il cambio tema non modifica `ReadingLocation`, `BookLayout`, bookmark, ricerca o progress;
- il tema predefinito resta la palette semantica validata in M2.4.
