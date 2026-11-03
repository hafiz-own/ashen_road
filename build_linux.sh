#!/usr/bin/env bash
# Builds the Linux player. Usage: ./build_linux.sh [folder]   (default Builds/Linux)
set -euo pipefail
cd "$(dirname "$0")"
./unity.sh -quit -executeMethod AshenRoad.EditorTools.BuildScript.BuildLinux -buildFolder "${1:-Builds/Linux}" -logFile Logs/build_linux.log
grep -E "Ashen Road build" Logs/build_linux.log || true
