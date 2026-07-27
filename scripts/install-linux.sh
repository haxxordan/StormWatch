#!/usr/bin/env bash
set -euo pipefail

storm_repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
mtga_game_dir="${MTGA_GAME_DIR:-${XDG_DATA_HOME:-$HOME/.local/share}/Steam/steamapps/common/MTGA}"
plugin_project="$storm_repo_dir/src/StormWatch/StormWatch.csproj"
plugin_output="$storm_repo_dir/src/StormWatch/bin/Release/netstandard2.1/StormWatch.dll"
plugin_destination="$mtga_game_dir/BepInEx/plugins/StormWatch/StormWatch.dll"

if [[ ! -f "$mtga_game_dir/MTGA.exe" ]]; then
  echo "MTGA.exe was not found in: $mtga_game_dir" >&2
  echo "Set MTGA_GAME_DIR to Arena's steamapps/common/MTGA directory." >&2
  exit 1
fi

if [[ ! -f "$mtga_game_dir/BepInEx/core/BepInEx.dll" ]]; then
  echo "BepInEx 5 is not installed in: $mtga_game_dir" >&2
  exit 1
fi

dotnet build "$plugin_project" -c Release -p:GameDir="$mtga_game_dir"
install -Dm644 "$plugin_output" "$plugin_destination"

echo "Installed StormWatch:"
echo "  $plugin_destination"
echo "Restart MTG Arena to load it."
