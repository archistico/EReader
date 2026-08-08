# Project Handoff — EReader M3.9 Hotfix 1 CA1859 Return-Type Analyzer Alignment Candidate

## Baseline

- baseline autoritativa validata: **M3.8 Hotfix 1 — Diagnostics Foundation & CA1859 Analyzer Alignment**;
- gate validato: `M3.8 HOTFIX 1 VALIDATION PASSED` (08/08/2026);
- candidate corrente: **M3.9 Hotfix 1 — CA1859 Return-Type Analyzer Alignment**;
- gate candidate: `M3.9 HOTFIX 1 VALIDATION PASSED`.

## Hotfix 1 — motivo

La prima candidate M3.9 ha fallito il build locale esclusivamente per `CA1859`: il metodo privato `EpubContainer.OpenValidatedEntry(...)` dichiarava `Stream` pur restituendo sempre `ValidatedZipEntryStream`. La Hotfix 1 usa il tipo concreto richiesto dall’analyzer; API pubbliche e comportamento M3.9 restano invariati.

## Obiettivo M3.9

Trattare l'EPUB come input non attendibile senza cambiare il modello di lettura. Un errore attribuibile in modo deterministico all'archivio/documento deve restare dentro il boundary EPUB e diventare diagnostica `DocumentUnreadable`; un bug interno inatteso non deve essere mascherato.

## Modifiche produttive principali

### Container/ZIP

- `EpubContainerLimits`: 256 MiB per entry, 2 GiB cumulativi dichiarati, ratio 500:1 da 16 MiB;
- `EpubContainerErrorCode`: nuovi codici 21–25 per oversize/ratio/tipo entry speciale/incoerenza;
- `ValidatedZipEntryStream`: wrapper read-only che traduce corruption/unsupported compression e valida la lunghezza a EOF;
- entry ZIP Unix di tipo speciale, inclusi symlink, rifiutate;
- prefissi drive/schema rifiutati anche dopo percent-decoding controllato (`C%3A/...`) e nei nomi ZIP;
- nessuna estrazione filesystem.

### OPF/URI

- manifest remoto: allow-list `http`/`https`;
- `file:` conserva il rifiuto storico; `data:`, `javascript:`, `ftp:` e altri schemi sono `UnsupportedRemoteResourceScheme`;
- fallback chain bounded a 64 passaggi, cycle detection invariato.

### Content

- UTF-8/UTF-16 strict;
- control character XML proibiti rifiutati come `InvalidXhtml`;
- budget Content preesistenti invariati.

### Validation

`EpubPublicationValidator.Validate(EpubContainer)` cattura `EpubContainerException` anche se emerge durante Protection/Package/Navigation/Content e la proietta come diagnostica Container `Invalid`. Non viene aggiunto un catch-all.

## Architettura

M3.9 resta confinata a `EbookReader.Epub` per ZIP, path e URI. Domain/Application/Layout non conoscono `ZipArchive`, compression ratio o policy OCF. La tassonomia M3.8 resta format-neutral nell'Application layer e il bridge resta nel CLI.

ADR: `docs/adr/0053-defensive-epub-input-stays-virtual-and-bounded.md`.

## Validazione

Eseguire da estrazione pulita:

```bat
.\validate.cmd
```

Gate atteso:

```text
M3.9 HOTFIX 1 VALIDATION PASSED
```

Conteggio statico atteso: 473 Fact + 5 Theory + 19 InlineData = 492 casi.

## Prossimo punto dopo validazione

**M3.10 — EPUB Recovery & Degraded Reading**.

M3.10 deve definire una matrice esplicita problema → recovery/esito (risorsa opzionale mancante, immagine corrotta, TOC assente, capitolo problematico, ecc.) senza indebolire i guardrail M3.9 e senza guessing silenzioso. Lo stato persistente valido deve essere aggiornato solo dopo un'apertura/operazione riuscita secondo il contratto definito.
