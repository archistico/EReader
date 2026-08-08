# Architecture Decision Records

Gli ADR sono documenti autoritativi: una decisione `Accepted` vincola l'implementazione finché un ADR successivo non la sostituisce esplicitamente.

Formato adottato: numero progressivo, titolo, stato, data, contesto, decisione, conseguenze e alternative considerate. Per nuove decisioni usare [`ADR-TEMPLATE.md`](ADR-TEMPLATE.md).

| ADR | Decisione | Stato |
|---|---|---|
| [0001](0001-dotnet-10-and-csharp-14.md) | .NET 10 e C# 14 | Accepted |
| [0002](0002-epub-only-non-drm-scope.md) | Solo EPUB reflowable non-DRM | Accepted |
| [0003](0003-format-neutral-domain.md) | Domain indipendente da EPUB | Accepted |
| [0004](0004-layered-dependency-direction.md) | Dipendenze a strati verso il Domain | Accepted |
| [0005](0005-terminal-gui-as-outer-adapter.md) | Terminal.Gui 2.x solo nella TUI | Accepted |
| [0006](0006-owned-epub-parser-and-anglesharp-boundary.md) | Parser EPUB proprietario, AngleSharp confinato | Accepted |
| [0007](0007-logical-reading-location.md) | Posizione di lettura logica | Accepted |
| [0008](0008-search-before-layout.md) | Ricerca prima del layout | Accepted |
| [0009](0009-json-first-persistence.md) | Persistenza JSON iniziale | Accepted |
| [0010](0010-no-drm-circumvention.md) | Nessuna gestione/aggiramento DRM | Accepted |
| [0011](0011-stable-domain-identities-and-logical-offsets.md) | Identificatori tipizzati e offset logici UTF-16 | Accepted |
| [0012](0012-linear-semantic-blocks-and-inline-tree.md) | Blocchi semantici lineari + albero inline | Accepted |
| [0013](0013-resource-descriptors-without-payload.md) | Resource descriptor senza payload nel Domain | Accepted |
| [0014](0014-ocf-virtual-paths-and-no-filesystem-extraction.md) | Path OCF virtuali, nessuna estrazione filesystem | Accepted |
| [0015](0015-strict-bounded-ocf-bootstrap.md) | Bootstrap OCF stretto, bounded e diagnostica strutturata | Accepted |
| [0016](0016-opf-intermediate-model-stays-in-epub-adapter.md) | Modello OPF intermedio confinato all’adapter EPUB | Accepted |
| [0017](0017-opf-url-resolution-and-bounded-xml.md) | Risoluzione URL OPF su OCF e XML bounded | Accepted |
| [0018](0018-source-normalized-version-strict-epub-navigation.md) | Navigazione EPUB normalizzata e strict-by-version | Accepted |
| [0019](0019-bounded-navigation-targets-and-ncx-doctype.md) | Target navigation OCF bounded e DOCTYPE NCX sicuro | Accepted |
| [0020](0020-anglesharp-html-parser-only-at-epub-content-boundary.md) | AngleSharp solo al boundary EPUB Content | Accepted |
| [0021](0021-xhtml-anchors-map-to-logical-reading-locations.md) | Anchor XHTML → ReadingLocation logica | Accepted |
| [0022](0022-targetless-navigation-grouping-is-format-neutral.md) | Navigation grouping targetless format-neutral | Accepted |
| [0023](0023-stable-ingestion-result-and-diagnostic-taxonomy.md) | Risultato ingestione stabile e diagnostica | Accepted |
| [0024](0024-inspect-protection-metadata-without-decryption.md) | Ispezione protection metadata senza decrittazione | Accepted |
| [0025](0025-non-paginated-console-projection-before-layout.md) | Proiezione console non paginata prima del layout | Accepted |
| [0026](0026-cli-streams-and-exit-codes.md) | stdout/stderr ed exit code CLI stabili | Accepted |
| [0027](0027-deterministic-layout-over-graphemes-and-terminal-cells.md) | Layout deterministico su grapheme e celle terminale | Accepted |
| [0028](0028-visual-lines-retain-logical-source-ranges.md) | VisualLine conserva intervalli logici UTF-16 | Accepted |
| [0029](0029-logical-chapter-navigation-separated-from-layout-navigation.md) | Navigazione logica separata dal layout | Accepted |
| [0030](0030-terminal-gui-reader-is-a-thin-outer-adapter.md) | Terminal.Gui reader come outer adapter sottile | Accepted |
| [0031](0031-fullscreen-default-with-explicit-plain-mode.md) | Fullscreen default, `--plain` esplicito | Accepted |
| [0032](0032-resize-reflows-from-body-viewport-and-preserves-logical-location.md) | Resize dal body viewport, ReadingLocation invariata | Accepted |
| [0033](0033-versioned-atomic-json-reading-state.md) | Stato lettura JSON versionato e atomico | Accepted |
| [0034](0034-resume-requires-path-book-id-and-valid-logical-location.md) | Resume con path + BookId + location valida | Accepted |
| [0035](0035-single-line-navigation-scrolls-a-sliding-viewport.md) | Navigazione per riga con viewport mobile | Accepted |
| [0036](0036-tui-separators-remain-outside-book-layout.md) | Separatori TUI fuori dal BookLayout | Accepted |
| [0037](0037-interactive-toc-is-a-projection-over-domain-navigation.md) | TOC interattivo come proiezione della navigazione Domain | Accepted |
| [0038](0038-metadata-view-projects-only-format-neutral-domain-metadata.md) | Vista metadata solo da BookMetadata format-neutral | Accepted |
| [0039](0039-search-operates-on-logical-domain-text-before-layout.md) | Ricerca sul testo logico Domain prima del layout | Accepted |
| [0040](0040-logical-bookmarks-are-multi-book-persistent-state.md) | Bookmark logici persistenti multi-book | Accepted |
| [0041](0041-semantic-inline-styles-remain-format-neutral-until-tui.md) | Stili inline semantici fino al boundary TUI | Accepted |
| [0042](0042-terminal-gui-2417-custom-schemes-use-setscheme.md) | Terminal.Gui 2.4.17: schemi custom via SetScheme | Accepted |

| [0043](0043-stable-progress-uses-logical-utf16-content.md) | Progresso stabile dal contenuto logico UTF-16 | Accepted |

| [0044](0044-recent-library-is-bounded-logical-json-state.md) | Libreria recente come stato JSON logico bounded | Accepted |
| [0045](0045-library-search-is-transient-ranked-application-state.md) | Ricerca libreria transiente e classificata nell’Application layer | Accepted |
| [0046](0046-reader-themes-map-semantic-roles-only-in-tui.md) | Temi reader come mapping TUI dei ruoli semantici | Accepted |
| [0047](0047-user-preferences-are-separate-versioned-configuration.md) | Preferenze utente in config JSON separata dallo stato lettura | Accepted |

## Stati

- **Proposed** — in discussione.
- **Accepted** — vincolante.
- **Superseded** — sostituito da ADR successivo.
- **Deprecated** — decisione non più raccomandata ma ancora presente per compatibilità.
- **Rejected** — valutato e non adottato.
