# ADR-0047 — User preferences are separate versioned configuration

- Status: Accepted and validated
- Date: 2026-08-08

## Context

EReader already persists book-specific state in `state.json`. M3.2 adds themes and M3.3 adds configurable reader shortcuts. Theme/keymap are user preferences, not properties of a book and not logical reading state.

Mixing them into the reading-state schema would couple UI customization to `ReadingLocation`, bookmarks and history, and would force unrelated state migrations.

## Decision

Introduce an independent `config.json`, schema version 1, owned by the CLI outer adapter.

It stores:

- stable theme id;
- printable reader-command aliases.

It does not store book state.

Printable aliases are single-grapheme, case-sensitive and collision-free. Missing bindings inherit the built-in defaults. Special navigation/control keys remain fixed and available as escape hatches.

`config.json` is bounded to 64 KiB and uses the same same-directory atomic-write pattern as reading state, but with an independent store and schema.

## Consequences

- `state.json` schema 3 remains unchanged;
- themes remain outside Domain/Application/Layout;
- invalid UI configuration cannot invalidate book state;
- future preference migrations can evolve without touching reading-history migrations;
- users cannot currently rebind special keys such as Esc, F1, Enter or arrow keys; that is deliberate safety scope for M3.3.
