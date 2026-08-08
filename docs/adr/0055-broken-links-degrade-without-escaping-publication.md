# ADR-0055 — Broken links degrade without escaping the publication

**Status:** Accepted for M3.11 candidate

## Context

M3.5/M3.6 made internal hyperlinks and note references actionable. M3.10 introduced deterministic degraded reading, but a missing hyperlink fragment could still make otherwise readable primary content fail, and a single broken TOC target could discard the whole TOC. External scheme allow-lists also existed independently at ingestion and OS-handoff boundaries.

## Decision

- Keep `EpubBookReader.Read(...)` strict for parser contracts.
- In the recovery-aware reader, preserve inline text but omit `HyperlinkSpan` when an internal target cannot be resolved safely.
- Treat invalid/traversing OCF references as broken links at this granular recovery boundary; never resolve them through the host filesystem.
- Recover a broken TOC target without dropping the whole TOC: omit an invalid leaf, or preserve a parent with valid children as a non-navigable grouping node.
- Centralize actionable external schemes in format-neutral `ExternalLinkPolicy`: `http`, `https`, `mailto`.
- Both EPUB ingestion and CLI OS handoff must call the shared policy.
- Do not probe external URLs over the network.
- `ReaderSession` validates an internal target before mutating `ReadingLocation` or its transient back-stack.

## Consequences

A publication may be `Valid` with recoverable link diagnostics. Broken links cannot create invalid Domain targets, access arbitrary host paths, or cause shell handoff through unsupported schemes. Navigation remains deterministic and the Domain invariant that internal targets are valid `ReadingLocation`s is preserved.

## Rejected alternatives

- Reject the entire EPUB for one broken link: too destructive for readable books.
- Guess a nearby anchor/path: non-deterministic and potentially unsafe.
- Keep separate allow-lists in EPUB and CLI: risks policy drift.
- Check URL availability/reputation online: adds network side effects and does not prove safety.
