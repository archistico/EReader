#!/usr/bin/env sh
set -eu

command -v dotnet >/dev/null 2>&1 || {
    echo "ERROR: dotnet non trovato nel PATH." >&2
    exit 1
}

echo "[1/11] Restore"
dotnet restore EbookReader.sln

echo "[2/11] Build Release"
dotnet build EbookReader.sln -c Release --no-restore

echo "[3/11] Test Release"
dotnet test --solution EbookReader.sln -c Release --no-build

echo "[4/11] CLI help smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --help >/dev/null

echo "[5/11] CLI version smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --version >/dev/null

echo "[6/11] CLI foundation-info smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --foundation-info

echo "[7/11] First readable EPUB plain smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --plain test-books/m1.0-smoke.epub >/dev/null

echo "[8/11] M3.4 image EPUB plain smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --plain test-books/m3.4-image-smoke.epub >/dev/null

echo "[9/11] M3.5 hyperlink EPUB plain smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --plain test-books/m3.5-link-smoke.epub >/dev/null

echo "[10/11] Library history smoke"
EREADER_STATE_FILE="${TMPDIR:-/tmp}/ereader-m30-validation-$$.json" dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --history >/dev/null
rm -f "${TMPDIR:-/tmp}/ereader-m30-validation-$$.json"

echo "[11/11] Preferences config smoke"
export EREADER_CONFIG_FILE="${TMPDIR:-/tmp}/ereader-m33-config-$$.json"
rm -f "$EREADER_CONFIG_FILE"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --init-config >/dev/null
test -f "$EREADER_CONFIG_FILE"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --config-path >/dev/null
rm -f "$EREADER_CONFIG_FILE"
unset EREADER_CONFIG_FILE

echo
echo "M3.5 HOTFIX 1 VALIDATION PASSED"
