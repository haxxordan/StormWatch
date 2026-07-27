#!/usr/bin/env bash
set -euo pipefail

storm_repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
mtga_game_dir="${MTGA_GAME_DIR:-${XDG_DATA_HOME:-$HOME/.local/share}/Steam/steamapps/common/MTGA}"
artifact_dir="$storm_repo_dir/artifacts/StormWatch"
archive_path="$storm_repo_dir/artifacts/StormWatch-linux-bepinex.tar.gz"

dotnet build "$storm_repo_dir/src/StormWatch/StormWatch.csproj" \
  -c Release \
  -p:GameDir="$mtga_game_dir"

install -Dm644 \
  "$storm_repo_dir/src/StormWatch/bin/Release/netstandard2.1/StormWatch.dll" \
  "$artifact_dir/BepInEx/plugins/StormWatch/StormWatch.dll"
install -Dm644 "$storm_repo_dir/README.md" "$artifact_dir/README.md"

(
  cd "$artifact_dir"
  tar -czf "$archive_path" BepInEx README.md
)

echo "Created $archive_path"
