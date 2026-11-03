#!/usr/bin/env bash
# Builds the Linux player to Builds/Linux/AshenRoad.x86_64
set -euo pipefail
cd "$(dirname "$0")"
./unity.sh -quit -executeMethod AshenRoad.EditorTools.BuildScript.BuildLinux -logFile Logs/build_linux.log
grep -E "Ashen Road build" Logs/build_linux.log || true
