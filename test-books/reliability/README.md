# M3.13 reliability corpus

These fixtures are intentionally malformed, degraded or security-sensitive. They are test data, not sample books.

| Fixture | Expected validator outcome | Required diagnostic | Purpose |
|---|---|---|---|
| valid-minimal.epub | Valid | none | readable control |
| missing-navigation.epub | Valid | ER-EPUB-RECOVERY-NAVIGATION-001 | degraded without TOC |
| missing-image.epub | Valid | ER-EPUB-RECOVERY-RESOURCE-001 | missing referenced image |
| broken-internal-link.epub | Valid | ER-EPUB-RECOVERY-LINK-001 | broken anchor |
| unsafe-external-link.epub | Valid | ER-EPUB-SECURITY-LINK-001 | unsafe scheme suppression |
| missing-container.epub | Invalid | ER-EPUB-CONTAINER-011 | missing container.xml |
| malformed-container.epub | Invalid | ER-EPUB-CONTAINER-013 | malformed container.xml |
| missing-opf.epub | Invalid | ER-EPUB-CONTAINER-019 | declared rootfile absent |
| malformed-opf.epub | Invalid | ER-EPUB-PACKAGE-002 | malformed OPF XML |
| missing-primary-chapter.epub | Invalid | ER-EPUB-CONTENT-003 | primary spine content absent |
| malformed-primary-xhtml.epub | Invalid | ER-EPUB-CONTENT-004 | malformed primary XHTML |
| invalid-utf8-primary.epub | Invalid | ER-EPUB-CONTENT-004 | invalid UTF-8 content |
| unsupported-encryption.epub | Unsupported | ER-EPUB-PROTECTION-UNSUPPORTED-001 | unsupported content encryption |
| duplicate-entry.epub | Invalid | ER-EPUB-CONTAINER-010 | duplicate ZIP/OCF path |
| path-traversal-entry.epub | Invalid | ER-EPUB-CONTAINER-009 | traversal entry |
| drive-prefixed-entry.epub | Invalid | ER-EPUB-CONTAINER-009 | drive/schema-like archive path |
| symlink-entry.epub | Invalid | ER-EPUB-CONTAINER-024 | Unix special/symlink entry |
| compression-ratio-bomb.epub | Invalid | ER-EPUB-CONTAINER-023 | pathological compression ratio |
| oversized-entry-metadata.epub | Invalid | ER-EPUB-CONTAINER-021 | declared entry above 256 MiB |
| truncated-zip.epub | Invalid | ER-EPUB-CONTAINER-001 | truncated ZIP directory |
