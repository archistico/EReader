# ADR-0020 — AngleSharp solo al boundary EPUB Content

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

M0.6 deve interpretare Content Document XHTML reali. Implementare un parser HTML/XHTML completo internamente produrrebbe complessità e gestione errori non pertinenti al dominio del reader. Al tempo stesso il modello interno non deve dipendere dal DOM di una libreria esterna.

## Decisione

`EbookReader.Epub` referenzia AngleSharp e soltanto il namespace `EbookReader.Epub.Content` può usare tipi AngleSharp. `EpubBookReader` usa `HtmlParser` per ottenere un DOM tollerante e lo converte immediatamente in draft EPUB-interni e quindi in tipi `EbookReader.Domain`.

Non vengono usati `BrowsingContext`, loader di rete, JavaScript o AngleSharp.Css. Il source viene letto esclusivamente tramite `EpubContainer.OpenEntry`.

La versione attivata in M0.6 è AngleSharp 1.7.1 tramite Central Package Management.

## Conseguenze

- il Domain resta privo di dipendenze HTML/EPUB;
- il parser beneficia dell'error recovery HTML5 di AngleSharp;
- una futura sostituzione di AngleSharp resta confinata all'adapter EPUB;
- CSS e browser behavior non diventano implicitamente parte del contratto di EReader.

## Alternative considerate

- parser XHTML solo `System.Xml`: respinto per tolleranza insufficiente sui libri reali;
- DOM AngleSharp esposto al Domain: respinto perché viola ADR-0003;
- browser embedded: respinto perché incompatibile con lo scope CLI/TUI e con la separazione del layout.
