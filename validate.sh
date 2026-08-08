#!/usr/bin/env sh
set -eu

command -v dotnet >/dev/null 2>&1 || {
    echo "ERROR: dotnet non trovato nel PATH." >&2
    exit 1
}

echo "[1/16] Restore"
dotnet restore EbookReader.sln

echo "[2/16] Build Release"
dotnet build EbookReader.sln -c Release --no-restore

echo "[3/16] Test Release"
dotnet test --solution EbookReader.sln -c Release --no-build

echo "[4/16] CLI help smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --help >/dev/null

echo "[5/16] CLI version smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --version >/dev/null

echo "[6/16] CLI foundation-info smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --foundation-info

echo "[7/16] First readable EPUB plain smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --plain test-books/m1.0-smoke.epub >/dev/null

echo "[8/16] M3.4 image EPUB plain smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --plain test-books/m3.4-image-smoke.epub >/dev/null

echo "[9/16] M3.5 hyperlink EPUB plain smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --plain test-books/m3.5-link-smoke.epub >/dev/null

echo "[10/16] M3.6 notes EPUB plain smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --plain test-books/m3.6-notes-smoke.epub >/dev/null

echo "[11/16] M3.10 degraded recovery smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --plain test-books/m3.10-recovery-smoke.epub >/dev/null

echo "[12/16] M3.11 link integrity smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --plain test-books/m3.11-link-integrity-smoke.epub >/dev/null


echo "[13/16] M3.12 diagnostics modifier smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --diagnostics --plain test-books/m1.0-smoke.epub >/dev/null

echo "[14/16] M3.13 reliability corpus inventory"
test -f test-books/reliability/README.md
_corpus_count=$(find test-books/reliability -maxdepth 1 -type f -name '*.epub' | wc -l | tr -d ' ')
test "$_corpus_count" = "20" || {
    echo "ERROR: corpus M3.13 atteso=20 EPUB, trovato=$_corpus_count." >&2
    exit 1
}

echo "[15/16] Library history smoke"
EREADER_STATE_FILE="${TMPDIR:-/tmp}/ereader-m30-validation-$$.json" dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --history >/dev/null
rm -f "${TMPDIR:-/tmp}/ereader-m30-validation-$$.json"

echo "[16/16] Preferences config smoke"
export EREADER_CONFIG_FILE="${TMPDIR:-/tmp}/ereader-m33-config-$$.json"
rm -f "$EREADER_CONFIG_FILE"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --init-config >/dev/null
test -f "$EREADER_CONFIG_FILE"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --config-path >/dev/null
rm -f "$EREADER_CONFIG_FILE"
unset EREADER_CONFIG_FILE

echo
echo "M3.13 HOTFIX 4 VALIDATION PASSED"
