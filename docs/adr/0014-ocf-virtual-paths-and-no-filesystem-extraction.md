# ADR-0014 — Path OCF virtuali e nessuna estrazione sul filesystem

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Un EPUB è un contenitore ZIP, ma il modello OCF definisce un filesystem virtuale root-relative e case-sensitive. Estrarre preventivamente il contenuto in una directory temporanea introdurrebbe dipendenze dal filesystem host, collisioni di casing e soprattutto una superficie Zip-Slip/path traversal non necessaria.

## Decisione

EReader non estrae l'EPUB sul filesystem durante il parsing. `EbookReader.Epub` lavora direttamente su `ZipArchive` in modalità read-only e usa `OcfPath` come rappresentazione canonica dei path interni.

I nomi reali delle entry ZIP e i riferimenti URL presenti nei documenti EPUB sono trattati come due input diversi:

- le entry ZIP sono file path OCF e non vengono percent-decoded;
- i riferimenti da `META-INF` vengono percent-decoded per segmento e normalizzati;
- `.` viene eliminato;
- `..` può risalire solo entro la root virtuale;
- un traversal oltre la root viene rifiutato;
- slash/backslash percent-encoded dentro un segmento vengono rifiutati;
- lookup e deduplicazione sono ordinali e case-sensitive.

## Conseguenze

- nessun Zip-Slip tramite estrazione;
- comportamento indipendente dal filesystem Windows/Linux/macOS;
- casing EPUB preservato;
- i futuri parser OPF/XHTML riceveranno sempre path OCF canonici;
- eventuali funzioni di export saranno casi d'uso separati e dovranno avere una propria policy di sicurezza.

## Alternative considerate

### Estrarre tutto in una directory temporanea

Scartato: aumenta I/O, lascia artefatti temporanei e crea problemi di sicurezza e portabilità inutili.

### Usare direttamente stringhe ZIP

Scartato: non distingue i file path reali dai riferimenti URL percent-encoded e rende fragile la risoluzione relativa futura.
