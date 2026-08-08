# EPUB Recovery Policy

**Stato:** policy target per M3.10, da rendere eseguibile e testata.  
**Baseline:** M3.7 Hotfix 1 VALIDATED.

## Principio

EReader deve recuperare soltanto quando la continuazione è deterministica e non altera in modo ambiguo il significato del libro.

Ogni problema deve produrre uno dei seguenti esiti concettuali:

```text
CONTINUE
CONTINUE_WITH_WARNING
CONTINUE_DEGRADED
REJECT_DOCUMENT
INTERNAL_ERROR
```

## Matrice iniziale

| Condizione | Target | Recovery |
|---|---|---|
| Cover assente | `CONTINUE_WITH_WARNING` o silenzioso se realmente opzionale | nessuna cover |
| CSS assente/non supportato | `CONTINUE_WITH_WARNING` | testo semantico invariato |
| Metadata opzionali assenti | `CONTINUE_WITH_WARNING` o `CONTINUE` | campi non disponibili |
| TOC assente ma spine valido | `CONTINUE_DEGRADED` | lettura senza TOC |
| Immagine locale assente | `CONTINUE_DEGRADED` | placeholder/alt text |
| Immagine corrotta/non decodificabile | `CONTINUE_DEGRADED` | placeholder + diagnostica |
| Risorsa remota non recuperata | `CONTINUE_DEGRADED` | placeholder; nessun fetch |
| Link interno verso risorsa assente | `CONTINUE_WITH_WARNING` | posizione invariata |
| Fragment/anchor inesistente | `CONTINUE_WITH_WARNING` | posizione invariata |
| Rimando nota rotto | `CONTINUE_WITH_WARNING` | posizione e back-stack invariati |
| Singolo Content Document malformato | da definire con test M3.10 | saltare solo se la policy dimostra che il libro resta leggibile senza guessing |
| Spine item assente | da definire con test M3.10 | recovery solo se non rende ambiguo il reading order |
| `container.xml` inutilizzabile | `REJECT_DOCUMENT` | nessuna |
| Package Document OPF assente/non determinabile | `REJECT_DOCUMENT` | nessuna |
| Nessun elemento leggibile nello spine | `REJECT_DOCUMENT` | nessuna |
| ZIP/OCF strutturalmente non utilizzabile | `REJECT_DOCUMENT` | nessuna |
| Contenuto essenziale realmente cifrato/non supportato | `REJECT_DOCUMENT` / `Unsupported` | nessuna circumvention |
| Violazione di sicurezza (traversal, path arbitrario, ecc.) | `REJECT_DOCUMENT` o risorsa rifiutata secondo il boundary | mai aggirare il guardrail |
| Eccezione inattesa EReader | `INTERNAL_ERROR` | containment M3.12, non guessing |

La tabella è deliberatamente prudente. M3.10 dovrà trasformare le righe ancora "da definire" in regole verificabili.

## Link: transazione logica

Seguire un link interno deve essere trattato come una piccola transazione:

1. identificare il link;
2. validare il target;
3. solo dopo il successo aggiungere l'origine al back-stack;
4. solo dopo il successo cambiare `ReadingLocation`.

Se il target fallisce, origine e stack devono restare invariati.

## Apertura libro: commit tardivo

L'apertura dovrebbe considerarsi completata soltanto quando esiste un `Book` utilizzabile e la sessione può essere inizializzata.

Prima del commit:

- non sostituire lo stato dell'ultimo libro valido;
- non creare annotazioni/bookmark impliciti;
- non persistire posizioni parziali;
- liberare risorse temporanee se l'apertura fallisce.

## Risorse mancanti

Una risorsa mancante non autorizza EReader a cercare alternative fuori dal package o su Internet. I fallback devono provenire esclusivamente dal contratto EPUB supportato o da placeholder locali del reader.

## Capitoli problematici

La policy definitiva M3.10 deve distinguere:

- capitolo supplementare non utilizzabile;
- capitolo primary non utilizzabile;
- uno spine con altri capitoli validi;
- uno spine che, dopo gli errori, non contiene più contenuto significativo.

Il criterio non deve essere semplicemente "catch exception e continua". Deve essere deterministico e coperto dal corpus M3.13.

## Persistenza

Recovery e diagnostica sono indipendenti dalla geometria del terminale. Non devono introdurre pagina, riga o viewport in `state.json`.

Le annotazioni M3.7 continuano a essere validate contro `BookId` e `ReadingLocation`; un libro rifiutato non deve causare migrazioni distruttive dello stato.
