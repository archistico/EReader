# EReader

EReader è un lettore di libri **EPUB 2/3 reflowable** pensato per il terminale. Offre una modalità fullscreen semplice, mantiene la posizione di lettura tra una sessione e l'altra e funziona senza convertire o modificare il file EPUB originale.

## Cosa offre

- lettura fullscreen nel terminale;
- indice interattivo e navigazione per capitoli;
- ricerca nel testo;
- bookmark persistenti;
- evidenziazioni e note personali;
- progresso di lettura stabile;
- cronologia e libreria recente;
- temi e tasti configurabili;
- link interni, note EPUB e ritorno alla posizione precedente;
- apertura esplicita di link esterni `http`, `https` e `mailto`;
- anteprima di immagini raster locali;
- diagnostica chiara per EPUB danneggiati, incompleti o non supportati;
- protezioni contro path traversal, ZIP anomali e altre strutture EPUB ostili.

EReader non supporta DRM, PDF, MOBI, AZW3 o FB2.

## Installazione

### Da una GitHub Release

1. Scarica l'archivio adatto al tuo sistema:
   - `EReader-<versione>-win-x64.zip` per Windows 64 bit;
   - `EReader-<versione>-linux-x64.tar.gz` per Linux 64 bit.
2. Estrai l'archivio in una cartella a tua scelta.
3. Avvia EReader dal terminale.

Le release prodotte dagli script del progetto sono **self-contained**: non richiedono un'installazione separata di .NET.

Windows:

```bat
ereader.exe "C:\Libri\libro.epub"
```

Linux:

```sh
./ereader "/home/utente/Libri/libro.epub"
```

Se Linux non conserva il bit eseguibile dopo l'estrazione:

```sh
chmod +x ereader
```

### Dal codice sorgente

Per compilare il repository è richiesto il **.NET 10 SDK**.

```sh
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -- libro.epub
```

## Uso principale

```text
ereader libro.epub            apre il reader fullscreen
ereader --resume              riapre l'ultimo libro alla posizione salvata
ereader --library             apre la libreria recente
ereader --history             stampa la cronologia recente
ereader --plain libro.epub    stampa il contenuto leggibile su stdout
ereader --help                mostra tutti i comandi
```

Se EReader segnala un errore interno e servono dettagli tecnici:

```text
ereader --diagnostics libro.epub
```

Un errore interno di EReader viene distinto da un EPUB realmente illeggibile: il programma non attribuisce automaticamente al libro un problema che potrebbe invece essere del reader.

## Comandi durante la lettura

| Tasto | Azione |
|---|---|
| `↑` / `↓` | riga precedente / successiva |
| `PgUp` / `PgDn` | pagina precedente / successiva |
| `Space` | pagina successiva |
| `[` / `]` | capitolo precedente / successivo |
| `g` / `G` | inizio / fine capitolo |
| `t` o `Tab` | indice |
| `/` | ricerca |
| `n` / `N` | risultato successivo / precedente |
| `b` | aggiunge o rimuove un bookmark |
| `B` | elenco bookmark |
| `m` | metadati |
| `c` | cambia tema |
| `Enter` | segue un link o una nota; altrimenti apre l'immagine corrente |
| `Backspace` | torna indietro dopo un link interno |
| `F2` | aggiunge/rimuove un'evidenziazione |
| `F3` | aggiunge/modifica una nota personale |
| `F4` | elenco annotazioni |
| `F1` | aiuto |
| `q` | esce e salva la posizione |

I tasti stampabili possono essere personalizzati; frecce, `PgUp/PgDn`, `Space`, `Tab`, `Enter`, `Backspace`, `Esc` e `F1-F4` restano sempre disponibili.

## Dove salva i dati

EReader salva localmente posizione di lettura, bookmark, note, evidenziazioni, cronologia e preferenze. Non modifica il file EPUB.

Per vedere o inizializzare la configurazione:

```text
ereader --config-path
ereader --init-config
```

I percorsi possono essere sovrascritti con `EREADER_STATE_FILE` e `EREADER_CONFIG_FILE`, utile soprattutto per installazioni portabili o test.

## EPUB problematici

EReader tenta di continuare quando il problema è non essenziale, ad esempio una risorsa opzionale mancante. Se invece la struttura necessaria alla lettura è realmente compromessa, rifiuta il singolo libro e mostra una diagnostica esplicita.

Il principio del progetto è:

> **Un EPUB può essere illeggibile. EReader no.**

Il reader tratta ogni EPUB come input non attendibile, non estrae l'archivio sul filesystem e non esegue codice contenuto nel libro.

## Documentazione

La documentazione completa è indicizzata in [`docs/README.md`](docs/README.md). Per compatibilità e limitazioni EPUB vedere [`docs/EPUB_COMPATIBILITY.md`](docs/EPUB_COMPATIBILITY.md).

Per chi sviluppa o prepara una release:

- [`docs/VALIDATION.md`](docs/VALIDATION.md)
- [`docs/RELEASE.md`](docs/RELEASE.md)
- [`docs/ROADMAP.md`](docs/ROADMAP.md)
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
