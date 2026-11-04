#!/usr/bin/env bash
# Builds the mac player. Usage: ./build_mac.sh [folder]   (default Builds/macOS). Needs the matching build module in Unity Hub.
set -euo pipefail
cd "$(dirname "$0")"
./unity.sh -quit -executeMethod AshenRoad.EditorTools.BuildScript.BuildMac -buildFolder "${1:-Builds/macOS}" -logFile Logs/build_mac.log
grep -E "Ashen Road build" Logs/build_mac.log || true
