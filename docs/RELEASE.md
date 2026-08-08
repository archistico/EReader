# Preparare una GitHub Release

Il repository include script che producono archivi **self-contained** pronti da allegare a una GitHub Release. Gli script non eseguono upload e non richiedono token o credenziali GitHub.

## Windows

Dalla radice del repository:

```bat
publish-release.cmd 1.0.0
```

Lo script:

1. pulisce tutte le cartelle `bin` e `obj`;
2. esegue il gate completo `validate.cmd`;
3. interrompe immediatamente la release se il gate fallisce;
4. pubblica il CLI come single-file self-contained per `win-x64` e `linux-x64`;
5. copia il `README.md` dentro ogni pacchetto;
6. crea gli asset in `artifacts\release\v<versione>\`.

Asset prodotti:

```text
EReader-<versione>-win-x64.zip
EReader-<versione>-linux-x64.tar.gz
```

Esempio:

```text
artifacts/release/v1.0.0/EReader-1.0.0-win-x64.zip
artifacts/release/v1.0.0/EReader-1.0.0-linux-x64.tar.gz
```

La versione può essere passata anche con il prefisso `v`; lo script lo normalizza.

## Linux

È disponibile l'equivalente:

```sh
./publish-release.sh 1.0.0
```

Richiede `dotnet`, `tar` e `zip`. Anche questa variante esegue prima `clean.sh` e `validate.sh`.

## Pulizia manuale

Per eliminare tutte le cartelle `bin` e `obj` dei progetti e dei test senza toccare sorgenti, configurazione o `artifacts/`:

Windows:

```bat
clean.cmd
```

Linux:

```sh
./clean.sh
```

Gli script cercano ricorsivamente soltanto directory chiamate esattamente `bin` o `obj`.

## Perché il gate è obbligatorio

Lo script non offre una modalità `skip validation`: un archivio destinato a GitHub non deve essere generato da una working tree che non supera il gate cumulativo.

Questo è particolarmente importante durante le stacked candidate: fino a quando M3.12/M3.13 non sono validate, lo script release terminerà al primo problema rilevato dal gate.

## Caricamento su GitHub

Gli archivi sotto `artifacts/release/` sono gli asset da allegare manualmente alla Release corrispondente. Il repository non automatizza la creazione del tag o l'upload, così autenticazione, note di release e scelta del tag restano operazioni esplicite.

Non vengono generati file checksum/hash.
