# M1.3/M1.4 — Terminal.Gui 2.x Reader TUI

## Obiettivo

M1.3 collega il `Book` format-neutral e i contratti M1.1/M1.2 a una prima interfaccia fullscreen realmente utilizzabile.

La TUI non è un secondo layout engine. È una proiezione del `BookLayout` corrente e conserva come stato soltanto una `ReadingLocation`.

## Avvio

```bat
ereader "C:\Libri\libro.epub"
```

Per il rendering lineare storico:

```bat
ereader --plain "C:\Libri\libro.epub"
```

## Struttura visuale

```text
┌ EReader ───────────────────────────────────────────────────────────────┐
│ Titolo — Autore   Cap. 3/21   Pag. 12/84                              │
│                                                                        │
│ contenuto della pagina prodotto da DeterministicLayoutEngine           │
│ ...                                                                    │
│                                                                        │
│ ↑/k ↓/j riga  PgUp/h PgDn/l/Space pagina  [ ] capitolo  ...           │
└────────────────────────────────────────────────────────────────────────┘
```

L'header espone titolo/autore, capitolo primario e pagina del layout corrente. La pagina è effimera; non costituisce una posizione persistibile.

## Comandi M1.3

| Tasto | Azione |
|---|---|
| `↑`, `k` | riga precedente |
| `↓`, `j` | riga successiva |
| `PgUp`, `h` | pagina precedente |
| `PgDn`, `l`, `Space` | pagina successiva |
| `[` | capitolo precedente |
| `]` | capitolo successivo |
| `g` | inizio capitolo |
| `G` | fine capitolo |
| `F1`, `?` | mostra/nasconde help |
| `q`, `Esc` | esce |

## Boundary

```text
Terminal.Gui
    ↓
ReaderWindow
    ↓
ReaderSession
  ↙          ↘
Layout     Application
   \          /
       Domain
```

`ReaderWindow` può conoscere Terminal.Gui, ma non EPUB. `ReaderSession` non conosce Terminal.Gui né EPUB.

## Viewport e resize M1.4

M1.3 crea un viewport iniziale di bootstrap dalle dimensioni del terminale. M1.4 Hotfix 2 sincronizza il reflow tramite `_body.ViewportChanged`, che Terminal.Gui 2.4.17 emette dopo l’aggiornamento della geometria reale del body.

Se width o height cambiano, `ReaderSession.Reflow` ricostruisce il `BookLayout` e conserva esattamente la stessa `ReadingLocation`. Pagina e riga possono cambiare e restano effimere.

Dettagli: [`RESIZE_STABILITY.md`](RESIZE_STABILITY.md).

## Help

F1 o `?` sostituisce temporaneamente il body con la pagina help. Chiudere l'help non modifica la `ReadingLocation`.

## Non obiettivi

M1.3/M1.4 non introducono:

- persistenza dell'ultima posizione;
- TOC interattivo;
- ricerca;
- bookmark;
- temi configurabili;
- immagini terminal-native.

Questi appartengono alle milestone successive.


## M2.0 Hotfix 1 — scorrimento per riga visibile

Le azioni `↑/↓` e `k/j` modificano da M1.2 una `ReadingLocation` stabile. La prima TUI renderizzava però sempre `LayoutPage` intera, quindi il testo restava identico finché il movimento non oltrepassava il confine di pagina.

Hotfix 1 rende la `ReadingLocation` anche l'ancora della finestra visibile:

```text
ReadingLocation corrente
        ↓
LayoutLocationResolver
        ↓
riga visuale corrente
        ↓
viewport.Height righe da quel punto
```

PgUp/PgDn continuano invece a saltare alla prima riga logica della pagina adiacente.


## M2.0 Hotfix 2 — separatori visuali

La TUI separa esplicitamente le tre aree principali:

```text
Titolo — Autore   Cap. x/y   Pag. n/m
──────────────────────────────────────
contenuto del libro
...
──────────────────────────────────────
↑/k ↓/j ... q/Esc esci
```

Le due righe orizzontali appartengono esclusivamente al chrome della TUI e non al `BookLayout`. Il body occupa quindi le righe comprese fra i due separatori; il suo `Viewport` reale continua a essere la sola geometria usata da `ReaderSession.Reflow`.


## M2.1 — Interactive TOC — VALIDATED

Il reader espone ora `t`/`Tab` per aprire il `TableOfContents` format-neutral nello stesso body. La gerarchia viene mostrata tramite indentazione; la selezione salta i grouping node senza target. `Enter` aggiorna direttamente la `ReadingLocation` del target e torna alla lettura. `Esc` chiude l'indice senza modificare la posizione.

Dettagli: [`INTERACTIVE_TOC.md`](INTERACTIVE_TOC.md).


## M2.2 — Metadata View

`m` apre la vista dei metadata nello stesso body del reader. I dati provengono esclusivamente da `BookMetadata` e `BookId` format-neutral.

```text
m                 apre/chiude metadata
↑/↓ oppure j/k      scroll una riga
PgUp/PgDn           scroll una pagina
Esc                 chiude metadata
```

Il formatter è indipendente da Terminal.Gui e usa la misura in celle di `TerminalCellWidth`; resize e scroll della vista non modificano mai la `ReadingLocation`.


## M2.3 — Search pre-layout

```text
/                 apre il prompt di ricerca
Enter             esegue
Backspace         elimina l'ultimo grapheme
Esc               annulla il prompt
n                 risultato successivo
N                 risultato precedente
```

Il prompt usa la status bar e non sostituisce il contenuto del libro. Dopo la ricerca, l'header mostra query e indice del match. I risultati sono `ReadingLocation` prodotte dall'Application layer prima del layout; la View non cerca nelle righe visuali.


## M2.4 — Bookmark

```text
b                 aggiunge/rimuove bookmark alla location corrente
B                 apre/chiude elenco bookmark
↑/↓ o j/k          selezione elenco
PgUp/PgDn          scorrimento elenco
Enter              salta al bookmark
d                  elimina bookmark selezionato
Esc                chiude elenco
```

L'elenco è una proiezione TUI di `ReadingLocation`; JSON e filesystem non sono responsabilità di `ReaderWindow`.


## M2.5 — Stable Progress

La vista normale aggiunge una percentuale logica accanto alla pagina, ad esempio `Pag. 12/84   37.4%`. La pagina deriva dal `BookLayout` e può cambiare dopo resize; la percentuale deriva da `BookProgressIndex` e dalla `ReadingLocation` e deve restare identica. Le overlay TOC/metadata/bookmark mantengono i propri header specializzati.


## M3.3 — Keymap configurabile

Da M3.3 gli alias stampabili mostrati da help/footer derivano da `config.json`. Le frecce, PgUp/PgDn, Space, Tab, Enter, Backspace, Esc e F1 restano scorciatoie speciali fisse (Backspace acquisisce anche il ritorno hyperlink in M3.5). Il file di configurazione è separato dallo stato di lettura; vedi `CONFIGURATION_KEYMAP.md`.


## M3.4 — Immagini

Il reader resta text-first. Un `ImageBlock` continua a essere rappresentato dal placeholder generato dal layout usando alt text/didascalia. Quando la `ReadingLocation` corrente appartiene a quel blocco, l'header aggiunge `IMG <media-type>` e il footer mostra `Enter immagine`.

`Enter` nella vista di lettura normale apre esplicitamente JPEG/PNG/GIF/WebP locali nel viewer associato dal sistema. Negli overlay mantiene invece i significati precedenti (apri voce TOC/bookmark, conferma ricerca). SVG e risorse remote non vengono aperti. La preview non cambia `ReadingLocation`, non viene ripetuta al resize e non viene persistita.

Dettagli e limiti: `IMAGES.md` e ADR-0048.


## M3.5 — Hyperlink interattivi

Nella lettura normale `Enter` dà priorità a un hyperlink azionabile. La `ReadingLocation` esatta dentro un link ha precedenza; altrimenti viene scelto il primo hyperlink che interseca la riga visuale corrente. I link interni saltano alla `ReadingLocation` Domain target e attivano `Backspace indietro`; i link esterni `http`/`https`/`mailto` vengono passati all'applicazione associata dal sistema operativo. Se la riga non offre link, `Enter` conserva l'anteprima immagine M3.4. Header e footer segnalano dinamicamente link e back-stack disponibili.


## M3.6 — Note e rimandi

Un hyperlink Domain con `HyperlinkRole.NoteReference` viene mostrato come `NOTA`; `Enter` apre la nota e `Backspace` ritorna al testo attraverso lo stesso stack logico M3.5. Nessun popup o layout speciale viene persistito.


## M3.7 — Highlights & Personal Notes

- F2: toggle highlight della riga logica corrente.
- F3: prompt inline per nota personale alla ReadingLocation corrente; Enter salva, Esc annulla, testo vuoto elimina.
- F4: overlay annotazioni; frecce/jk selezionano, PgUp/PgDn scorrono, Enter salta, `d` (binding delete corrente) elimina.
- Header: `EVID` se la riga corrente interseca un highlight e `NOTA PERSONALE` se esiste una nota esatta alla posizione.
- Il rendering highlight è line-level, ma il range persistito resta preciso e indipendente dal layout.
