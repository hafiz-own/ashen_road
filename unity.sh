#!/usr/bin/env bash
# Runs the Unity editor headless on this project. Usage: ./unity.sh [extra Unity args...]
# Set UNITY to override the editor path.
set -euo pipefail
cd "$(dirname "$0")"
UNITY="${UNITY:-$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity}"
mkdir -p Logs
exec "$UNITY" -batchmode -projectPath "$PWD" "$@"
