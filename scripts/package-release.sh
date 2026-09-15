#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
version="0.5.13"
release_dir="$root/release"
staging_dir="$release_dir/.package-staging"
archive="$release_dir/SmeltAndFuel-$version.zip"
plugin="$root/bin/Release/netstandard2.1/SmeltAndFuel.dll"
plugin_dir="$staging_dir/BepInEx/plugins/themockingjet-SmeltAndFuel"

dotnet build "$root/SmeltAndFuel.csproj" --configuration Release

mkdir -p "$plugin_dir"
cp "$root/manifest.json" "$root/README.md" "$root/CHANGELOG.md" "$root/icon.png" "$staging_dir/"
cp "$plugin" "$plugin_dir/"
rm -f "$archive"

(
    cd "$staging_dir"
    zip -q -r "$archive" manifest.json README.md CHANGELOG.md icon.png BepInEx/plugins/themockingjet-SmeltAndFuel/SmeltAndFuel.dll
)

unzip -t "$archive"
unzip -l "$archive"
