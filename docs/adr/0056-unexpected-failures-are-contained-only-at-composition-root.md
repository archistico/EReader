# ADR-0056 — Unexpected failures are contained only at the composition root

- **Status:** Accepted — validated cumulatively with M3.13 Hotfix 1
- **Date:** 2026-08-08

## Context

M0.7–M3.11 distinguono già errori del documento, condizioni non supportate, recovery deterministiche e I/O previsto. Rimane però possibile che un bug o una violazione d'invariante inattesa risalga fino al processo CLI. Lasciare terminare il processo senza classificazione contraddice il principio “Un EPUB può essere illeggibile. EReader no.”; catturare `Exception` nei parser, invece, nasconderebbe bug classificandoli come input difettoso.

## Decision

Il catch generale è ammesso in un solo punto: `CliEntryPoint.Run(...)`, il composition root più esterno e testabile.

Il boundary:

1. non modifica i catch tipizzati di EPUB/persistence/configuration;
2. non converte l'eccezione in `EpubValidationStatus.Invalid` o `Unsupported`;
3. crea `ReaderOperationStatus.InternalFailure` + `ER-READER-INTERNAL-001`;
4. restituisce exit code 6;
5. mostra un messaggio primario leggibile;
6. espone `Exception.ToString()` soltanto con `--diagnostics`;
7. non contiene `OutOfMemoryException`, `StackOverflowException` e `AccessViolationException` come normali failure recuperabili.

La soppressione CA1031 sul metodo è intenzionale e documentata; un architecture test garantisce che nei sorgenti produttivi esista una sola occorrenza di `catch (Exception` e che `EpubPublicationValidator` continui a non usarla.

## State safety

Il containment non tenta un rollback ad hoc. Lo stato di lettura mantiene il modello di commit atomico già validato: il nuovo snapshot viene salvato soltanto dopo una sessione terminata e la scrittura JSON usa temp + flush + rename. Un'operazione che fallisce prima del commit non deve sostituire lo stato persistente precedente.

## Consequences

Positive:

- gli errori interni hanno outcome e codice distinti dai document failure;
- l'utente riceve un messaggio comprensibile invece di un crash grezzo;
- troubleshooting disponibile senza rendere lo stack trace il messaggio primario;
- parser e recovery EPUB conservano failure model strict.

Trade-off:

- una sessione TUI che genera un errore interno termina l'operazione corrente; M3.12 non ricostruisce in-process la finestra;
- le mutazioni in-memory non ancora committate non vengono promosse a stato persistente;
- process-critical failures restano fuori dal contratto di continuità ordinaria.
