# M3.3 — Configurable Keymap & Preferences

M3.3 separa definitivamente **preferenze utente** e **stato di lettura**.

- `state.json` resta lo stato dei libri: ultimo libro, `ReadingLocation`, bookmark e cronologia;
- `config.json` contiene il tema preferito e gli alias stampabili della TUI reader;
- la percentuale, pagina, riga e viewport non vengono persistite in nessuno dei due file.

## Percorso

Percorso predefinito:

```text
%LOCALAPPDATA%\EReader\config.json       Windows
$XDG_DATA_HOME/EReader/config.json*      dipende da LocalApplicationData .NET
```

Il percorso effettivo può essere letto con:

```text
ereader --config-path
```

Per test, portable install o automazione:

```text
EREADER_CONFIG_FILE=<percorso>
```

Per creare il file predefinito senza sovrascrivere un file esistente:

```text
ereader --init-config
```

## Schema 1

Esempio:

```json
{
  "schemaVersion": 1,
  "theme": "semantic-dark",
  "keymap": {
    "PreviousLine": "k",
    "NextLine": "j",
    "PreviousPage": "h",
    "NextPage": "l",
    "PreviousChapter": "[",
    "NextChapter": "]",
    "ChapterStart": "g",
    "ChapterEnd": "G",
    "ToggleToc": "t",
    "Search": "/",
    "NextSearchResult": "n",
    "PreviousSearchResult": "N",
    "ToggleBookmark": "b",
    "OpenBookmarks": "B",
    "ToggleMetadata": "m",
    "CycleTheme": "c",
    "Help": "?",
    "Quit": "q",
    "DeleteBookmark": "d"
  }
}
```

Tema ammesso:

```text
semantic-dark
paper-light
monochrome
```

## Regole keymap

I binding configurabili sono intenzionalmente limitati ai tasti **stampabili**:

- confronto case-sensitive (`b` e `B` sono distinti);
- un singolo grapheme per comando;
- spazi e caratteri di controllo non sono ammessi;
- due comandi non possono usare lo stesso binding;
- le proprietà mancanti ereditano il default;
- un nome comando sconosciuto rende il file invalido.

Restano sempre disponibili come escape hatch i tasti speciali canonici:

```text
Frecce
PgUp / PgDn
Space
Tab
Enter
Esc
F1
Backspace (prompt ricerca e, da M3.5, ritorno hyperlink)
```

Questa scelta evita che una configurazione personalizzata renda il reader inutilizzabile.

## Persistenza del tema

M3.2 introduceva i temi come stato transiente. In M3.3, quando il lettore viene chiuso dopo aver ciclato il tema con il comando `CycleTheme`, il nuovo `theme` viene scritto atomicamente in `config.json`.

Il tema non entra mai nel `Book`, nel `BookLayout` o nella `ReadingLocation`.

## Robustezza

`config.json`:

- schema versionato indipendentemente da `state.json`;
- limite 64 KiB;
- scrittura temp + flush-to-disk + rename same-directory;
- file assente = configurazione predefinita;
- file invalido = warning e fallback ai default per la sessione, senza bloccare la lettura;
- una configurazione invalida non viene sovrascritta automaticamente dalla sessione.


## M3.7 fixed annotation keys

F2 (highlight), F3 (personal note) e F4 (annotations list) sono special keys fissi, come F1/Enter/Esc. `config.json` resta schema 1 e i binding stampabili M3.3 non cambiano. Questa scelta evita collisioni/migrazioni per configurazioni create prima di M3.7.
