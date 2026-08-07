# ADR-0042 — Terminal.Gui 2.4.17 applica gli schemi custom tramite SetScheme

- **Stato:** Accepted
- **Data:** 2026-08-08

## Contesto

M2.4 Hotfix 1 ha introdotto una palette semantica corretta, ma usava la sintassi `Scheme = ...` su `Window`, `Label` e `ReaderBodyView`. Il build contro il package Terminal.Gui 2.4.17 ha dimostrato che quella surface API non espone una proprietà `Scheme` assegnabile. Il sorgente ufficiale del tag v2.4.17 espone invece `View.SetScheme(Scheme?)` e `View.GetScheme()`.

## Decisione

- Gli schemi custom EReader vengono applicati esclusivamente tramite `View.SetScheme(...)`.
- `ReaderColorPalette` continua a produrre oggetti `Scheme` e `Attribute`; la palette cromatica non cambia.
- `ReaderWindow`, `ReaderBodyView`, header/footer e separatori applicano il proprio schema dopo la costruzione della View.
- Non viene introdotta configurazione globale di `SchemeManager`: i colori restano locali al reader.

## Conseguenze

- Compatibilità esplicita con Terminal.Gui 2.4.17.
- Nessuna modifica a Domain, EPUB, Application, ReadingLocation, bookmark o JSON.
- Una futura migrazione Terminal.Gui dovrà verificare nuovamente la surface API prima di cambiare il meccanismo di theming.

## Alternative considerate

### Registrare schemi globali tramite SchemeManager

Rifiutato: sarebbe più invasivo e globale del necessario per una palette locale al reader.

### Disegnare anche header/footer/cornice manualmente

Rifiutato: duplicazione inutile; `SetScheme` è l'API nativa della versione fissata.
