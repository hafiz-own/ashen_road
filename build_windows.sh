#!/usr/bin/env bash
# Builds the windows player. Usage: ./build_windows.sh [folder]   (default Builds/Windows). Needs the matching build module in Unity Hub.
set -euo pipefail
cd "$(dirname "$0")"
./unity.sh -quit -executeMethod AshenRoad.EditorTools.BuildScript.BuildWindows -buildFolder "${1:-Builds/Windows}" -logFile Logs/build_windows.log
grep -E "Ashen Road build" Logs/build_windows.log || true
