#!/usr/bin/env bash
# open_vermin.sh - launches Unity at this project directly (no Hub)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$SCRIPT_DIR"
UNITY_HUB_DIR="${UNITY_HUB_DIR:-$HOME/Unity/Hub/Editor}"
UNITY_EXE="${UNITY_EXE:-}"

if [[ -z "$UNITY_EXE" && -d "$UNITY_HUB_DIR" ]]; then
  LATEST=$(ls -1 "$UNITY_HUB_DIR" 2>/dev/null | grep '^2023\.' | sort -V | tail -1 || true)
  [[ -n "$LATEST" && -x "$UNITY_HUB_DIR/$LATEST/Editor/Unity" ]] && UNITY_EXE="$UNITY_HUB_DIR/$LATEST/Editor/Unity"
fi

if [[ -z "$UNITY_EXE" && -d "/Applications/Unity/Hub/Editor" ]]; then
  LATEST=$(ls -1 /Applications/Unity/Hub/Editor 2>/dev/null | grep '^2023\.' | sort -V | tail -1 || true)
  [[ -n "$LATEST" ]] && UNITY_EXE="/Applications/Unity/Hub/Editor/$LATEST/Unity.app/Contents/MacOS/Unity"
fi

if [[ -z "$UNITY_EXE" || ! -x "$UNITY_EXE" ]]; then
  echo "[x] No 2023 LTS Unity editor found." >&2
  echo "    Install via Unity Hub, or: UNITY_EXE=/path/to/Unity ./open_vermin.sh" >&2
  exit 1
fi

[[ -d "$PROJECT_PATH/Assets" ]] || { echo "[x] No Assets/ - run ./setup_vermin.sh first"; exit 1; }

echo "[+] Unity:   $UNITY_EXE"
echo "[+] Project: $PROJECT_PATH"

case "${1:-open}" in
  open)
    exec "$UNITY_EXE" -projectPath "$PROJECT_PATH"
    ;;
  --setup|setup)
    exec "$UNITY_EXE" -projectPath "$PROJECT_PATH" -executeMethod Vermin.Editor.VerminProjectSetup.RunAllFromCli
    ;;
  --build-android|build-android)
    LOGFILE="$PROJECT_PATH/Logs/android_build_$(date +%Y%m%d_%H%M%S).log"
    mkdir -p "$PROJECT_PATH/Logs"
    echo "[+] Build log: $LOGFILE"
    exec "$UNITY_EXE" -projectPath "$PROJECT_PATH" -batchmode -nographics -quit \
      -buildTarget Android \
      -executeMethod Vermin.Editor.VerminProjectSetup.BuildAndroidFromCli \
      -logFile "$LOGFILE"
    ;;
  -h|--help|help)
    echo "Usage: ./open_vermin.sh [open|--setup|--build-android]"; exit 0;;
  *)
    echo "Unknown mode: $1"; exit 1;;
esac
