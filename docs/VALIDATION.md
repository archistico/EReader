# Validation — M3.11 Hotfix 1 Navigation Invariant Alignment

M3.11 è costruita esclusivamente sopra la baseline autoritativa validata **M3.10 Hotfix 2** (`M3.10 HOTFIX 2 VALIDATION PASSED`, 08/08/2026).

## Gate

Windows:

```bat
.\validate.cmd
```

Linux/macOS:

```sh
./validate.sh
```

Il gate esegue 14 step: restore, build Release, suite completa, help/version/foundation-info, smoke M1.0/M3.4/M3.5/M3.6, smoke recovery M3.10, smoke link-integrity M3.11, history e config.

Output finale atteso:

```text
M3.11 HOTFIX 1 VALIDATION PASSED
```

## Contratti M3.11

- `CliEntryPoint.Milestone == "M3.11"`;
- `state.json` resta schema 4 e `config.json` resta schema 1;
- `ExternalLinkPolicy` è format-neutral e consente solo `http`, `https`, `mailto`;
- adapter EPUB e `SystemExternalLinkService` usano la stessa allow-list;
- link `file:`, `javascript:`, `data:`, `ftp:` e schemi sconosciuti non sono azionabili;
- nessun `HttpClient`/`WebRequest` viene introdotto per validare URL;
- fragment/anchor interno irrisolvibile nel percorso recovery-aware preserva il testo e produce `ER-EPUB-RECOVERY-LINK-001`;
- `noteref` rotto produce la stessa diagnostica con wording nota-specifico;
- target locale fuori dal reading order o riferimento traversal/malformato non provoca accessi filesystem;
- target TOC rotto produce `ER-EPUB-RECOVERY-NAVIGATION-003`: la foglia senza figli viene omessa, il parent con figli validi resta come grouping node;
- percent-encoding valido dei fragment continua a risolversi correttamente;
- `ReaderSession` verifica il target prima di mutare lo stack; self-link/non-followable non cambiano posizione né back-stack;
- parser pubblico `EpubBookReader.Read(...)` resta strict;
- nessun catch-all `Exception` viene aggiunto alla facade EPUB.

## Smoke M3.11

`test-books/m3.11-link-integrity-smoke.epub` contiene intenzionalmente:

- un target TOC con fragment inesistente e uno valido;
- un hyperlink interno rotto;
- un `epub:type="noteref"` rotto;
- un `javascript:` non azionabile;
- un link HTTPS valido.

Il comando `--plain` deve terminare con exit code 0: il testo rimane leggibile e le anomalie di link sono diagnostiche, non motivo per rifiutare il libro.

## Conteggio statico

La candidate contiene:

```text
495 Fact
7 Theory
27 InlineData
524 casi attesi (Fact + InlineData)
```

Il conteggio autoritativo resta quello prodotto dal gate locale.

## Non incluso

M3.11 non implementa ancora:

- crash containment globale o conversione delle eccezioni interne in messaggi UX — M3.12;
- corpus esteso di EPUB corrotti e gate di affidabilità dedicato — M3.13;
- network reputation/checking degli URL;
- browser embedded o download di risorse remote;
- persistenza del back-stack o di URI esterni.
