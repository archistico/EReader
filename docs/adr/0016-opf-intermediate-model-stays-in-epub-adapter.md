# ADR-0016 — Il modello OPF intermedio resta nell'adapter EPUB

- **Stato:** Accepted
- **Data:** 2026-08-07

## Contesto

Il Package Document OPF contiene concetti specifici di EPUB: `manifest`, `spine`, `itemref`, `linear`, fallback, proprietà EPUB, NCX legacy, media overlay e URL di risorse. Il Domain M0.2 è deliberatamente indipendente dal formato sorgente.

## Decisione

M0.4 introduce `EpubPackageDocument` e tipi correlati esclusivamente in `EbookReader.Epub.Package`. Essi rappresentano fedelmente l'OPF ma non sono tipi Domain e non vengono referenziati dal Domain.

La futura conversione EPUB → Domain avverrà in una fase successiva, dopo navigation e XHTML semantic extraction.

## Conseguenze

- il Domain non acquisisce `manifest`, `spine`, `idref` o altri dettagli OPF;
- possiamo preservare informazioni necessarie alle milestone M0.5/M0.6;
- EPUB 2 ed EPUB 3 possono condividere lo stesso adapter senza cambiare il modello neutrale;
- eventuali formati futuri non dovranno simulare una struttura OPF.

## Alternative considerate

- mappare immediatamente OPF a `Book`: scartato perché il contenuto XHTML non è ancora interpretato;
- inserire concetti manifest/spine nel Domain: scartato perché violerebbe ADR-0003.
