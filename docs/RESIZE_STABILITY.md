# M1.4 — Resize Stability

## Obiettivo

M1.4 rende il reader fullscreen realmente responsive senza trasformare pagina o riga in stato persistente.

Quando Terminal.Gui cambia la geometria disponibile, EReader:

1. osserva `_body.ViewportChanged`, evento pubblico Terminal.Gui 2.4.17 emesso dopo l’aggiornamento del viewport;
2. legge il `Viewport` reale del body;
3. costruisce un nuovo `LayoutViewport`;
4. ricostruisce deterministicamente il `BookLayout`;
5. conserva esattamente la stessa `ReadingLocation`;
6. risolve la nuova pagina/riga soltanto come proiezione effimera.

## Invariante centrale

```text
prima del resize
ReadingLocation = S/B/offset
        ↓
nuovo viewport
        ↓
nuovo BookLayout
        ↓
dopo il resize
ReadingLocation = S/B/offset
```

`PageNumber`, `LineIndex` e il numero totale di pagine possono cambiare liberamente.

## Perché il body viewport

La dimensione rilevante non è la dimensione grezza della console, ma lo spazio effettivamente assegnato da Terminal.Gui al body dopo bordo, header e footer.

M1.4 usa quindi:

```text
_body.Viewport.Width
_body.Viewport.Height
```

Il valore letto da `Console.WindowWidth/Height` resta soltanto un bootstrap iniziale, necessario prima del primo layout Terminal.Gui.

## ReaderSession.Reflow

`ReaderSession` espone:

```csharp
bool Reflow(LayoutViewport viewport)
```

Il metodo è testabile senza Terminal.Gui e segue il contratto:

- viewport identico → `false`, nessuna nuova allocazione di layout;
- viewport diverso → nuovo `BookLayout` e `true`;
- `ReadingLocation` invariata;
- nessuna conversione posizione → pagina → location;
- nessuna persistenza di coordinate visuali.

## Stabilità e loop di layout

`ReaderWindow` usa un guard `_synchronizingViewport` e `ReaderSession.Reflow` è idempotente per viewport uguali.

Questo evita reflow ricorsivi quando il refresh del testo provoca ulteriori passaggi di layout/draw.

## Terminali molto piccoli

`LayoutViewport` richiede almeno due celle di larghezza e una riga. La factory TUI applica quindi:

```text
width  = max(2, body width)
height = max(1, body height)
```

Lo scopo è evitare crash durante resize estremi. La resa può ovviamente essere poco utile finché il terminale resta molto piccolo.

## Help

Il resize viene elaborato anche mentre l'help è visibile. Il layout del libro viene aggiornato in background, ma:

- l'help resta visibile;
- la `ReadingLocation` non cambia;
- chiudendo l'help viene mostrata la pagina corrispondente al nuovo viewport.

## Non obiettivi

M1.4 non introduce:

- persistenza su disco;
- TOC interattivo;
- ricerca;
- bookmark;
- stable progress;
- debounce temporale del resize;
- temi o keymap configurabile.

Il reflow è sincrono e deterministico. Ottimizzazioni prestazionali vengono introdotte solo se misure reali le rendono necessarie.
