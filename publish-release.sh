#!/usr/bin/env sh
set -eu

ROOT=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
VERSION=${1:-}

if [ -z "$VERSION" ]; then
    echo "Uso: ./publish-release.sh VERSION" >&2
    echo "Esempio: ./publish-release.sh 1.0.0" >&2
    exit 2
fi

case "$VERSION" in
    v*) VERSION=${VERSION#v} ;;
esac

command -v dotnet >/dev/null 2>&1 || { echo "ERROR: dotnet non trovato nel PATH." >&2; exit 1; }
command -v tar >/dev/null 2>&1 || { echo "ERROR: tar non trovato nel PATH." >&2; exit 1; }
command -v zip >/dev/null 2>&1 || { echo "ERROR: zip non trovato nel PATH." >&2; exit 1; }

cd "$ROOT"
./clean.sh
./validate.sh

OUT="$ROOT/artifacts/release/v$VERSION"
rm -rf -- "$OUT"
mkdir -p -- "$OUT"

publish_runtime() {
    rid=$1
    stage="$OUT/$rid"

    echo
    echo "Publish $rid..."
    dotnet publish src/EbookReader.Cli/EbookReader.Cli.csproj \
        -c Release \
        -r "$rid" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:DebugSymbols=false \
        -p:DebugType=None \
        -p:ContinuousIntegrationBuild=true \
        -p:Version="$VERSION" \
        -p:VersionSuffix= \
        -o "$stage"

    cp README.md "$stage/README.md"

    if [ "$rid" = "linux-x64" ]; then
        "$stage/ereader" --version >/dev/null
        (cd "$stage" && tar -czf "$OUT/EReader-$VERSION-linux-x64.tar.gz" .)
    else
        (cd "$stage" && zip -qr "$OUT/EReader-$VERSION-win-x64.zip" .)
    fi

    rm -rf -- "$stage"
}

publish_runtime win-x64
publish_runtime linux-x64

echo
echo "Release pronta in: $OUT"
echo "Asset GitHub:"
echo "  EReader-$VERSION-win-x64.zip"
echo "  EReader-$VERSION-linux-x64.tar.gz"
