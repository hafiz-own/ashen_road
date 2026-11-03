#!/usr/bin/env bash
# Regenerates data assets, prefabs and scenes from code and the level .txt files.
set -euo pipefail
cd "$(dirname "$0")"
./unity.sh -nographics -quit -executeMethod AshenRoad.EditorTools.ProjectAssetBuilder.BuildAll -logFile Logs/rebuild.log
grep -E "error CS|Exception|Builder:" Logs/rebuild.log || true
echo "Rebuild finished. Log: Logs/rebuild.log"
