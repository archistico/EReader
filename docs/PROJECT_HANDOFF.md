# Project Handoff — EReader M2.4 Hotfix 3

## Stato

- **Ultima baseline autoritativa validata:** M2.3 Hotfix 1 — Search pre-layout.
- Gate utente: `M2.3 HOTFIX 1 VALIDATION PASSED`.
- **Candidate corrente:** M2.4 Hotfix 3 — architecture contract alignment after semantic-color rendering.
- M2.4 è costruita esclusivamente sopra `EReader_M2.3_Hotfix1_SearchPreLayout_NET10_Candidate.zip` validata.

## M2.4

M2.4 introduce bookmark persistenti basati soltanto su `ReadingLocation`.

### TUI

- `b`: aggiunge/rimuove bookmark alla location corrente;
- `B`: apre/chiude elenco bookmark;
- `↑/↓` o `j/k`: selezione;
- `PgUp/PgDn`: scorrimento rapido;
- `Enter`: salto al bookmark;
- `d`: elimina bookmark;
- `Esc`: chiude elenco;
- `★` nell'header quando la location corrente è bookmarkata.

### Persistenza

- `state.json` passa a `schemaVersion: 2`;
- `lastBook` resta responsabile del resume;
- `bookmarks` è una collezione multi-book separata;
- ogni bookmark contiene path + BookId + ReadingLocation;
- schema 1 ancora leggibile come stato senza bookmark;
- scrittura atomica M2.0 invariata;
- max 1.000 bookmark/libro, 10.000 complessivi, 1 MiB file.

### Boundary

- Domain invariato;
- EPUB invariato;
- Layout invariato;
- `ReaderWindow` non accede al JSON;
- etichette/snippet bookmark sono proiezioni TUI effimere e non persistite.

## Gate

```text
.\validate.cmd
```

Esito atteso:

```text
M2.4 HOTFIX 3 VALIDATION PASSED
```

## Prossimo milestone

**M2.5 — Stable Progress**: percentuale di avanzamento logica, indipendente dal layout/viewport.

## M2.4 Hotfix 1

- Corregge CS0103 in `ReaderSession`: nessun alias `global::` dentro interpolazione.
- Mantiene il modello bookmark/schema 2 invariato.
- Preserva Strong/Emphasis in `VisualLine.StyleSpans` durante il wrapping.
- `ReaderBodyView` disegna heading cyan, strong verdi, emphasis gialli, testo bianco.
- La `Window` e i separatori usano grigio su nero; header/footer/body sovrascrivono con bianco.
- Domain ed EPUB restano estranei alla palette; `EbookReader.Layout` conserva solo semantica format-neutral.
- ADR-0041 e `docs/READER_COLORS.md` sono autoritativi per questa scelta.


## M2.4 Hotfix 2

- Il build Hotfix 1 ha confermato Domain/Application/Layout/EPUB verdi, ma il CLI si è fermato su 6 errori relativi a `Scheme = ...`.
- Terminal.Gui 2.4.17 espone `View.SetScheme(Scheme?)`; Hotfix 2 usa questa API su Window, ReaderBodyView, header/footer e separatori.
- Palette, style span semantici, bookmark e JSON schema 2 restano invariati.

## M2.4 Hotfix 3

- Build della Hotfix 2: riuscita.
- Test Hotfix 2: 394/395 passati; unico fallimento un architecture contract obsoleto, non codice produttivo.
- `ReaderWindow` non usa più `RenderCurrentViewport()` perché il rendering colorato passa `VisualLine[]` a `ReaderBodyView`.
- Il contratto è riallineato a `_body.ShowReaderLines(_session.GetCurrentViewportLines())`.
- Nessuna modifica produttiva rispetto alla Hotfix 2.
