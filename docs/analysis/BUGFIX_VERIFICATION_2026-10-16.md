# Verifica correzioni audit — 16 ottobre 2026

L'audit della revisione `8fe1d07` ha individuato quattro difetti runtime e un
test dipendente dalla data. Questa correzione aggiunge 23 casi eseguibili e
rende deterministico il test della cronologia esistente.

| Problema | Comportamento corretto | Verifiche |
|---|---|---|
| Annotazioni accettate oltre i limiti complessivi | Il composition root calcola il budget del libro escludendo le annotazioni dello stesso path che saranno sostituite. La sessione controlla numero di note, highlight e lunghezza complessiva del testo prima della mutazione. Gli altri libri sono preservati; modifiche rifiutate lasciano intatta la nota precedente. | [ReaderAnnotationSessionTests](../../tests/EbookReader.Cli.Tests/ReaderAnnotationSessionTests.cs): limite di testo di 131.072 caratteri, 500 note e 1.000 highlight distribuiti fra libri; modifica e cancellazione al limite; spazio liberato da una riduzione; sostituzione dell'identità allo stesso path; round trip JSON di note, bookmark e posizione dopo il rifiuto della nota eccedente. |
| Anchor inline vuoti verso blocchi inesistenti | Il parser conserva il blocco logico dei batch senza testo che registrano anchor. Link e TOC puntano a una posizione valida anche alla fine del documento. | [EpubBookReaderTests](../../tests/EbookReader.Epub.Tests/Content/EpubBookReaderTests.cs): `span` e `a` finali, link e TOC, anchor intermedio senza riutilizzo del blocco. [FirstReadableEpubTests](../../tests/EbookReader.Cli.Tests/FirstReadableEpubTests.cs): `--plain` restituisce 0 ed emette il testo. |
| Elementi null nello stato JSON | Bookmarks, history, highlights e notes producono `InvalidDataException`, gestita come stato non valido. | [ReadingStateTests](../../tests/EbookReader.Application.Tests/ReadingStateTests.cs): tutte e quattro le collezioni. [ReadingStateCliTests](../../tests/EbookReader.Cli.Tests/ReadingStateCliTests.cs): `--history` termina con 0 e `ER-STATE-LOAD-001`, senza errore interno e senza modificare il file. La modifica della variabile d'ambiente è isolata dalla parallelizzazione e ripristinata al termine. |
| Overflow della numerazione delle liste | Il contatore intermedio usa `Int64`; un ordinale ordinato non rappresentabile produce `InvalidContent`. L'ultimo elemento può avere `Int32.MaxValue` e la numerazione può essere ripresa con un `value` esplicito. | [EpubBookReaderTests](../../tests/EbookReader.Epub.Tests/Content/EpubBookReaderTests.cs): limite da `start` e da `li value`, massimo finale e reset esplicito. [FirstReadableEpubTests](../../tests/EbookReader.Cli.Tests/FirstReadableEpubTests.cs): esito EPUB non leggibile con exit code 3, senza errore interno. |
| Test cronologia legato all'orologio | Tutte le date del caso sono derivate da un unico riferimento fisso. L'ordinamento produttivo resta per timestamp. | [ReadingHistoryTests](../../tests/EbookReader.Application.Tests/ReadingHistoryTests.cs): `UpdatePlacesCurrentBookFirstAndReplacesSamePath`. |

## Esito

- Prima delle correzioni, i primi nuovi test hanno confermato 10 fallimenti
  delle riproduzioni runtime, oltre al fallimento già presente della cronologia.
- Dopo le correzioni: **586 test passati, 0 falliti, 0 ignorati**.
- `validate.cmd`: **tutti i 16 passi completati**, con
  `M3.13 HOTFIX 4 VALIDATION PASSED`.
- Build Release: **0 avvisi, 0 errori**.
- `git diff --check`: superato.

Le verifiche della sessione sono automatiche sul suo stato testabile e sul
salvataggio JSON; non simulano i tasti di un terminale interattivo reale.
Il gate include i test di layout, architettura e corpus EPUB preesistenti,
oltre agli smoke test CLI.
