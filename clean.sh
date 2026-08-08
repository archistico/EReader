#!/usr/bin/env sh
set -eu

ROOT=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$ROOT"

echo "Pulizia cartelle bin e obj..."
count=0

# Prima raccogliamo i percorsi, poi li rimuoviamo: evitare di modificare l'albero
# mentre find lo sta ancora attraversando rende il comportamento riproducibile.
tmp_file="${TMPDIR:-/tmp}/ereader-clean-$$.txt"
trap 'rm -f "$tmp_file"' EXIT HUP INT TERM
find . -type d \( -name bin -o -name obj \) -print > "$tmp_file"

while IFS= read -r directory; do
    [ -n "$directory" ] || continue
    echo "  Rimuovo $directory"
    rm -rf -- "$directory"
    count=$((count + 1))
done < "$tmp_file"

echo "Pulizia completata. Cartelle rimosse: $count"
