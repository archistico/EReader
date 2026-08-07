# ADR-0025 — Proiezione console non paginata prima del layout engine

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

M0.6 produce già un `Book` format-neutral completo. Prima di progettare wrapping, viewport e pagine è utile dimostrare che una pubblicazione EPUB supportata può attraversare l'intera pipeline fino a un output leggibile.

Se M1.0 introducesse contemporaneamente layout e TUI, un errore visibile a schermo sarebbe ambiguo: potrebbe provenire dal parser EPUB, dal mapping Domain, dal layout o dalla UI.

## Decisione

M1.0 introduce nel solo adapter CLI una proiezione plain-text deterministica del `Book` Domain.

- il rendering attraversa `ReadingOrder` nell'ordine Domain;
- heading e paragrafi usano `ContentText`;
- quote ricevono un prefisso `> ` per livello;
- liste ricevono marker `-` o ordinale e indentazione per profondità;
- `pre` preserva il testo;
- immagini diventano placeholder testuali basati su alt/caption;
- thematic break diventa `---`;
- non viene eseguito alcun wrapping dipendente dalla larghezza terminale;
- non esiste ancora il concetto di pagina visuale.

## Conseguenze

- `ereader libro.epub` diventa utile già in M1.0;
- la correttezza EPUB→Domain può essere verificata separatamente dal layout;
- l'output M1.0 non deve essere usato come base per posizione persistente o numero pagina;
- M1.1 potrà sostituire la proiezione diretta con il layout engine senza modificare il Domain.

## Alternative considerate

- introdurre direttamente Terminal.Gui fullscreen: respinto perché mescolerebbe troppi boundary;
- implementare wrapping nel CLI: respinto perché il wrapping appartiene a `EbookReader.Layout`;
- stampare il testo XHTML originale: respinto perché violerebbe il boundary format-neutral.
