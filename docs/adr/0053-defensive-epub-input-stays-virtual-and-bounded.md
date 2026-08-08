# ADR-0053 — Defensive EPUB input stays virtual and bounded

- **Status:** Accepted for M3.9 candidate
- **Date:** 2026-08-08

## Context

M0.3–M0.7 hanno già introdotto OCF virtuale, traversal rejection, XML bounded/no-XXE, parser EPUB separato e validation non-throwing per i failure attesi. Prima di M4.0 il reader deve però trattare esplicitamente l'archivio come input ostile: una ZIP può dichiarare payload enormi, compression ratio patologici, symlink o metodi non supportati; inoltre una corruzione può emergere solo quando una specifica entry viene decompressa.

## Decision

M3.9 mantiene l'intero file system EPUB virtuale e read-only. Nessuna entry viene estratta per il parsing.

Il Container applica guardrail globali prima dell'uso delle risorse:

- massimo 100.000 entry archivio;
- massimo 256 MiB decompressi dichiarati per singola entry;
- massimo 2 GiB decompressi dichiarati cumulativi;
- rapporto di compressione massimo 500:1 per entry da almeno 16 MiB;
- entry ZIP Unix di tipo speciale, inclusi symbolic link, rifiutate;
- path con traversal, separatori non ammessi e prefissi drive/schema rifiutati anche dopo percent-decoding controllato.

Le entry aperte sono avvolte da uno stream read-only validato che traduce corruption/unsupported compression in `EpubContainerException` e verifica la lunghezza dichiarata quando viene osservato EOF.

`EpubPublicationValidator` considera `EpubContainerException` un expected document failure anche quando emerge durante Protection, Package, Navigation o Content. Non cattura eccezioni runtime arbitrarie.

Per le risorse remote del manifest viene adottata una allow-list `http`/`https`; nessun URI causa network retrieval automatico. Le fallback chain OPF sono bounded a 64 passaggi oltre al cycle detection.

La decodifica dei Content Document UTF-8/UTF-16 è strict; byte invalidi e control character XML proibiti diventano failure Content stabili.

## Consequences

- un EPUB patologico viene rifiutato prima di decompressioni/allocation non bounded;
- la corruzione ZIP resta nel boundary EPUB e produce `DocumentUnreadable` tramite la foundation M3.8;
- Domain/Application/Layout restano ignari di ZIP, compression ratio e schemi EPUB;
- nessuna policy di recovery viene inventata in questa milestone;
- i limiti possono rifiutare pubblicazioni eccezionalmente grandi: M5.0 potrà rivalutarli con corpus reali senza rimuovere il principio bounded;
- il crash containment per bug interni resta M3.12.

## Alternatives considered

### Estrarre l'EPUB in una directory temporanea

Rifiutato: aumenta la superficie path traversal/symlink, richiede cleanup e crea scritture controllabili indirettamente dal documento.

### Affidarsi solo ai limiti dei singoli parser

Rifiutato: risorse non ancora consumate possono comunque dichiarare metadata ZIP patologici e alcune corruzioni emergono prima del parser specifico.

### Accettare qualsiasi URI assoluta perché non viene scaricata

Rifiutato: mantenere una allow-list esplicita riduce ambiguità future e impedisce che `file:`/script-like schemes diventino accidentalmente actionable in milestone successive.

### Catch-all nel validator

Rifiutato per M3.9: distinguere input difettoso da bug EReader resta essenziale; il containment di eccezioni inattese appartiene a M3.12.
