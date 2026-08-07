# M2.5 — Stable Progress

M2.5 aggiunge una percentuale di avanzamento logica che non dipende dal layout terminale.

## Obiettivo

La stessa `ReadingLocation` deve produrre la stessa percentuale con viewport diversi:

```text
ReadingLocation(section, block, offset)
        │
        ├── layout 40×10  → Pag. 18/74
        └── layout 120×40 → Pag.  9/38

Stable Progress → identico in entrambi i casi
```

## Unità logica

EReader usa code unit UTF-16 della proiezione Domain:

```text
ContentText.GetPlainText(block).Length
```

È la stessa unità di `ReadingLocation.CharacterOffset`.

Per esempio `A😀B` occupa quattro code unit UTF-16. Una location all'offset 3 è quindi dopo `A` e l'emoji, indipendentemente da quante celle terminale occupino.

## Formula

```text
consumed = testo dei blocchi precedenti + offset nel blocco corrente
progress = consumed / testo logico totale del ReadingOrder
```

`BookProgressIndex` precomputa gli offset assoluti di sezioni e blocchi una volta per sessione.

## Header TUI

La vista normale mostra:

```text
Titolo — Autore   Cap. 3/21   Pag. 12/84   37.4%
```

`Pag. 12/84` resta una coordinata effimera del layout; `37.4%` è invece logica e stabile.

## Persistenza

La percentuale non viene salvata. `state.json` continua a salvare soltanto la `ReadingLocation`; al riavvio M2.5 ricalcola la percentuale dal `Book` corrente.

## Casi limite

- inizio del primo reading section: `0.0%`;
- fine dell'ultimo blocco testuale: `100.0%`;
- libro senza testo logico: `0.0%`;
- sezioni supplementary: incluse perché appartengono al `Book.ReadingOrder`;
- resize/reflow: percentuale invariata alla stessa location.
