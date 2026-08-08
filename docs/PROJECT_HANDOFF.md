# Project Handoff — EReader M3.11 Hotfix 1 Navigation Invariant Alignment Candidate

## Baseline

- baseline autoritativa validata: **M3.10 Hotfix 2**;
- gate baseline: `M3.10 HOTFIX 2 VALIDATION PASSED` (08/08/2026);
- candidate corrente: **M3.11 Hotfix 1 — Navigation Invariant Alignment**;
- gate candidate: `M3.11 HOTFIX 1 VALIDATION PASSED`.

La candidate deve essere considerata non validata finché il gate locale non termina con la stringa sopra.

## Obiettivo

M3.11 impedisce che link e target di navigazione difettosi rendano inutilizzabile un EPUB altrimenti leggibile. La recovery è granulare e non autorizza guessing, filesystem esterno o rete.

## Decisioni implementate

### Hyperlink interni

Nel percorso `EpubBookReader.ReadRecovering(...)`:

- fragment inesistente;
- target fuori dal reading order;
- riferimento locale malformato;
- traversal/percent-encoding che esce dalla root OCF;

vengono convertiti in testo non azionabile + `EpubContentRecoveryKind.InternalHyperlinkDropped`. Il validator proietta l'issue in `ER-EPUB-RECOVERY-LINK-001` con severity EPUB `Error`, quindi `RecoverableError` reader-wide.

Il parser pubblico strict conserva i contratti precedenti e continua a poter lanciare `EpubContentException` per gli stessi input.

### Note

`epub:type="noteref"` continua a essere `HyperlinkRole.NoteReference` quando il target è valido. Se il target è rotto, il testo viene preservato e la diagnostica usa il wording `Rimando nota`.

### TOC

M3.10 poteva omettere l'intero TOC se un target era irrisolvibile. M3.11 introduce `BuildTableOfContentsRecovering(...)`: ogni nodo viene risolto indipendentemente. Un target rotto produce `ER-EPUB-RECOVERY-NAVIGATION-003`; se la voce è foglia viene omessa per rispettare l’invariante Domain, mentre se contiene figli validi resta come grouping node `Target == null`. Un grouping che perde tutti i figli durante la recovery viene omesso, così il Domain non riceve mai un nodo senza target e senza figli. Target validi fratelli/figli restano navigabili.

### Link esterni

`EbookReader.Domain.Content.ExternalLinkPolicy` centralizza la allow-list:

```text
http
https
mailto
```

`EpubBookReader` usa la policy prima di creare `ExternalLinkTarget`; `SystemExternalLinkService` la verifica nuovamente prima di `Process.Start`. Schemi non ammessi restano testo e, nel percorso recovery-aware, producono `ER-EPUB-SECURITY-LINK-001` Warning.

Nessun URL viene verificato via rete.

### Transazione ReaderSession

`FollowCurrentInternalHyperlink()` verifica:

1. esistenza del current internal hyperlink;
2. target diverso dalla posizione corrente;
3. appartenenza del target al `Book`;
4. solo dopo la validazione esegue push origine + salto.

Un self-link o un link non seguibile restituisce `false` senza cambiare posizione/back-stack.

## File principali M3.11

Produzione:

- `src/EbookReader.Domain/Content/ExternalLinkPolicy.cs`;
- `src/EbookReader.Epub/Content/EpubBookReader.cs`;
- `src/EbookReader.Epub/Content/EpubBookRecoveryResult.cs`;
- `src/EbookReader.Epub/Validation/EpubDiagnosticCodes.cs`;
- `src/EbookReader.Epub/Validation/EpubPublicationValidator.cs`;
- `src/EbookReader.Cli/Links/SystemExternalLinkService.cs`;
- `src/EbookReader.Cli/Tui/ReaderSession.cs`;
- `src/EbookReader.Cli/CliEntryPoint.cs`.

Test principali:

- `tests/EbookReader.Domain.Tests/Content/InlineContentTests.cs`;
- `tests/EbookReader.Epub.Tests/Content/EpubBookReaderTests.cs`;
- `tests/EbookReader.Epub.Tests/Validation/EpubPublicationValidatorTests.cs`;
- `tests/EbookReader.Cli.Tests/ReaderSessionTests.cs`;
- `tests/EbookReader.Architecture.Tests/ArchitectureContractTests.cs`.

Fixture:

- `test-books/m3.11-link-integrity-smoke.epub`.

ADR:

- `docs/adr/0055-broken-links-degrade-without-escaping-publication.md`.

## Invarianti da preservare

- Domain/Application/Layout non dipendono da `EbookReader.Epub`;
- nessuna coordinata layout persistita;
- `state.json` schema 4;
- `config.json` schema 1;
- back-stack runtime-only e bounded a 128;
- nessun fetch automatico;
- nessuna estrazione generale dell'EPUB;
- limiti M3.9 invariati;
- primary reading order ancora necessario;
- nessun catch-all di eccezioni interne prima di M3.12.

## Gate

```text
495 Fact
7 Theory
27 InlineData
524 casi attesi
14 step
```

Eseguire da estrazione pulita:

```bat
.\validate.cmd
```

## Prossimo punto dopo il gate

**M3.12 — Crash Containment & Diagnostics UX**.

Obiettivo: separare in modo visibile un `FatalDocumentError` da un guasto interno EReader, confinare il fallimento alla sessione/libro, preservare stato preesistente e mostrare dettagli tecnici separatamente senza trasformare eccezioni inattese in falsi errori EPUB.
