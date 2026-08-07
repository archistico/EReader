#!/usr/bin/env sh
set -eu

command -v dotnet >/dev/null 2>&1 || {
    echo "ERROR: dotnet non trovato nel PATH." >&2
    exit 1
}

echo "[1/8] Restore"
dotnet restore EbookReader.sln

echo "[2/8] Build Release"
dotnet build EbookReader.sln -c Release --no-restore

echo "[3/8] Test Release"
dotnet test --solution EbookReader.sln -c Release --no-build

echo "[4/8] CLI help smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --help >/dev/null

echo "[5/8] CLI version smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --version >/dev/null

echo "[6/8] CLI foundation-info smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --foundation-info

echo "[7/8] First readable EPUB plain smoke"
dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --plain test-books/m1.0-smoke.epub >/dev/null

echo "[8/8] Library history smoke"
EREADER_STATE_FILE="${TMPDIR:-/tmp}/ereader-m30-validation-$$.json" dotnet run --project src/EbookReader.Cli/EbookReader.Cli.csproj -c Release --no-build -- --history >/dev/null
rm -f "${TMPDIR:-/tmp}/ereader-m30-validation-$$.json"

echo
echo "M3.1 HOTFIX 1 VALIDATION PASSED"
