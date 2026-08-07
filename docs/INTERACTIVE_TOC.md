# M2.1 — Interactive TOC

**Stato:** VALIDATED — `M2.1 VALIDATION PASSED`

## Obiettivo

M2.1 rende interattivo il `TableOfContents` format-neutral già prodotto dalla pipeline EPUB, senza introdurre conoscenza di EPUB nella TUI.

## Comando

Nel reader fullscreen:

```text
t / Tab
```

apre o chiude l'indice.

## Interazione

```text
↑ / k              voce precedente
↓ / j              voce successiva
PgUp / PgDn        scorrimento rapido dell'indice
Enter              apre la voce selezionata
Esc                chiude l'indice senza cambiare posizione
t / Tab            chiude l'indice
q                  esce da EReader
```

La selezione considera solo le voci con target. I grouping node senza `ReadingLocation` restano visibili e contribuiscono alla gerarchia, ma vengono saltati durante il movimento della selezione.

## Modello

```text
Book.TableOfContents
        ↓
NavigationItem tree
        ↓
ReaderSession.FlattenTableOfContents
        ↓
ReaderTocEntry[]
├── Label
├── Depth
└── ReadingLocation?
        ↓
ReaderWindow overlay
```

`ReaderTocEntry` è una proiezione TUI e non sostituisce il modello Domain.

## Preselezione

Quando l'indice viene aperto, `ReaderSession.SuggestedTocEntryIndex` cerca la voce navigabile logicamente più vicina e non successiva alla `ReadingLocation` corrente.

L'ordinamento considera:

1. posizione della `ReadingSection` nel reading order;
2. posizione del `ContentBlock` nella sezione;
3. offset UTF-16 nel blocco.

Questo permette di preselezionare anche heading/anchor multipli nella stessa sezione.

## Salto

`Enter` chiama:

```text
ReaderSession.NavigateToTocEntry(index)
```

che aggiorna direttamente la `ReadingLocation` del target. Nessuna pagina o riga visuale viene usata come coordinate di navigazione.

Dopo il salto il normale `LayoutLocationResolver` determina la nuova posizione visuale nel layout corrente.

## Resize

L'indice usa il `_body.Viewport` già esistente. Se il terminale cambia dimensione:

- M1.4 continua a ricostruire il `BookLayout` sulla stessa `ReadingLocation`;
- il TOC ricalcola solo la propria finestra visibile;
- la voce selezionata resta visibile;
- nessun dato del TOC viene persistito.

## Persistenza

M2.1 non cambia `state.json`.

Dopo un salto via TOC, alla normale chiusura della TUI M2.0 salva la nuova `ReadingLocation` esattamente come per qualsiasi altro movimento.

## Boundary

`ReaderWindow` non conosce:

- `nav.xhtml`;
- NCX;
- href EPUB;
- manifest/spine;
- parser XHTML.

La TUI riceve esclusivamente il `Book` format-neutral attraverso `ReaderSession`.
