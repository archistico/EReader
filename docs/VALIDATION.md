# Validation — M2.4 Hotfix 3 Architecture contract alignment

## Baseline

M2.3 Hotfix 1 (`M2.3 HOTFIX 1 VALIDATION PASSED`) resta l’ultima baseline validata. La candidate M2.4 è costruita esclusivamente su quella base; M2.4 Hotfix 1 è costruita esclusivamente sulla candidate M2.4; Hotfix 2 è costruita esclusivamente sopra Hotfix 1 dopo i 6 errori CS0118/CS0103/CS0117 legati all’uso di `Scheme = ...` con Terminal.Gui 2.4.17.


## Hotfix 2

Il sorgente ufficiale del tag Terminal.Gui `v2.4.17` espone `View.SetScheme(Scheme?)` e `View.GetScheme()`, non una proprietà `Scheme` assegnabile. Hotfix 2 sostituisce quindi tutte le assegnazioni `Scheme = ReaderColorPalette...` con chiamate `SetScheme(...)` su `ReaderWindow`, `ReaderBodyView`, header, footer e separatori. Palette, style span e semantica bookmark restano invariati.

## Hotfix 1

La prima candidate M2.4 ha superato restore e compilazione di Domain/Application/Layout/EPUB ma il CLI si è fermato in `ReaderSession.cs` con CS0103 perché `global::` compariva dentro un'espressione interpolata. Hotfix 1 usa l'import normale `EbookReader.Application.State` e `ReadingBookmarkState.MaximumBookmarksPerBook`.

La stessa hotfix introduce il rendering cromatico richiesto, mantenendo colori e `Terminal.Gui.Drawing.Attribute` confinati in `EbookReader.Cli`. `VisualLine` conserva soltanto `VisualTextSpan`/`VisualTextStyle`.

Criteri aggiuntivi:

- heading cyan/azzurri;
- strong verdi + bold;
- emphasis gialli + italic;
- testo ordinario bianco;
- cornice e separatori grigi;
- style span corretti dopo wrapping, quote/list prefix e Unicode;
- nessun riferimento Terminal.Gui o nome colore in `EbookReader.Layout`;
- nessuna regressione bookmark/state schema 2.

## Gate Windows

```bat
.\validate.cmd
```

## Gate manuale

```bat
dotnet restore EbookReader.sln
dotnet build EbookReader.sln -c Release --no-restore
dotnet test --solution EbookReader.sln -c Release --no-build
```

Seguono gli smoke CLI `--help`, `--version`, `--foundation-info` e `--plain test-books\m1.0-smoke.epub`.

## Esito atteso

```text
M2.4 HOTFIX 3 VALIDATION PASSED
```

La suite contiene staticamente **379 `[Fact]` + 16 `[InlineData]`**, per **395 casi attesi**.


## M2.4 Hotfix 3

Il gate della Hotfix 2 ha compilato correttamente tutti i progetti e ha eseguito 395 test: 394 passati e un solo fallimento nel test architetturale `M20Hotfix1ReaderWindowUsesSlidingViewportForLineNavigation`. Il contratto verificava ancora la precedente chiamata `_session.RenderCurrentViewport()`, rimossa intenzionalmente quando M2.4 Hotfix 1 ha introdotto `ReaderBodyView` per preservare gli span semantici e applicare i colori.

Hotfix 3 non modifica codice produttivo. Il regression contract ora verifica la pipeline equivalente e corrente `_body.ShowReaderLines(_session.GetCurrentViewportLines())`, mantenendo anche le verifiche sui binding `j/k` e frecce. Analyzer e warnings-as-errors restano invariati.

Gate atteso:

```text
M2.4 HOTFIX 3 VALIDATION PASSED
```

## Criteri M2.4

- tutti i test M0→M2.3 restano verdi;
- `b` toggle bookmark logico e `B` apre l'elenco;
- bookmark iniziali ordinati secondo reading order;
- salto/eliminazione non usano coordinate di layout;
- reflow non modifica le ReadingLocation dei bookmark;
- JSON schema 2 serializza la libreria bookmark multi-book;
- schema 1 resta leggibile;
- bookmark di altri libri vengono preservati;
- bookmark stale dello stesso path con BookId vecchio non vengono riutilizzati;
- nessun `PageNumber`, `LineIndex`, `LayoutPosition` o viewport nello stato bookmark;
- `ReaderWindow` resta priva di persistenza/JSON;
- `--plain` resta stateless.
