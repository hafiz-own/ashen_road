#!/usr/bin/env bash
# Runs all EditMode and PlayMode tests headless and prints a summary. Results: TestResults/*.xml
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p TestResults
status=0
for platform in EditMode PlayMode; do
  echo "== $platform tests"
  ./unity.sh -runTests -testPlatform "$platform" -testResults "$PWD/TestResults/$platform.xml" \
    -logFile "Logs/tests_$platform.log" || status=1
  python3 - "$PWD/TestResults/$platform.xml" <<'PY' || status=1
import sys, xml.etree.ElementTree as ET
try:
    root = ET.parse(sys.argv[1]).getroot()
except Exception as e:
    print("   no results:", e); sys.exit(1)
a = root.attrib
print(f"   total {a.get('total')}  passed {a.get('passed')}  failed {a.get('failed')}  skipped {a.get('skipped')}")
for case in root.iter('test-case'):
    if case.attrib.get('result') == 'Failed':
        msg = case.find('.//message')
        print("   FAILED", case.attrib.get('fullname'), "-", (msg.text or '').strip().splitlines()[0] if msg is not None and msg.text else '')
sys.exit(0 if a.get('failed') == '0' else 1)
PY
done
exit $status
