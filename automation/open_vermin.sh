#!/usr/bin/env bash
# open_vermin.sh
# Launches Unity DIRECTLY at the Vermin-Exterminator project path.
# Bypasses Unity Hub entirely, so Hub cannot accidentally choose a wrong
# (nested) project folder.
#
# Usage:
#   ./open_vermin.sh                  # opens the project in the editor
#   ./open_vermin.sh --setup          # opens + auto-runs Vermin.Setup.RunAll
#   ./open_vermin.sh --build-android  # headless Android build (needs Android module)

set -euo pipefail

# -------- resolve paths --------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$SCRIPT_DIR"

# -------- auto-detect Unity editor --------
# Looks in the usual Unity Hub install location; picks the newest 2023.* LTS it finds.
UNITY_HUB_DIR="${UNITY_HUB_DIR:-$HOME/Unity/Hub/Editor}"

UNITY_EXE="${UNITY_EXE:-}"

if [[ -z "$UNITY_EXE" ]]; then
  if [[ -d "$UNITY_HUB_DIR" ]]; then
    # Pick newest 2023.x installation
    LATEST=$(ls -1 "$UNITY_HUB_DIR" 2>/dev/null | grep '^2023\.' | sort -V | tail -1 || true)
    if [[ -n "$LATEST" && -x "$UNITY_HUB_DIR/$LATEST/Editor/Unity" ]]; then
      UNITY_EXE="$UNITY_HUB_DIR/$LATEST/Editor/Unity"
    fi
  fi
fi

# macOS fallback
if [[ -z "$UNITY_EXE" && -d "/Applications/Unity/Hub/Editor" ]]; then
  LATEST=$(ls -1 /Applications/Unity/Hub/Editor 2>/dev/null | grep '^2023\.' | sort -V | tail -1 || true)
  if [[ -n "$LATEST" ]]; then
    UNITY_EXE="/Applications/Unity/Hub/Editor/$LATEST/Unity.app/Contents/MacOS/Unity"
  fi
fi

if [[ -z "$UNITY_EXE" || ! -x "$UNITY_EXE" ]]; then
  echo "[x] Could not auto-detect a 2023 LTS Unity editor." >&2
  echo "    Install one via Unity Hub, or set UNITY_EXE env var:" >&2
  echo "      UNITY_EXE=/path/to/Unity ./open_vermin.sh" >&2
  exit 1
fi

if [[ ! -d "$PROJECT_PATH/Assets" ]]; then
  echo "[x] $PROJECT_PATH does not look like a Unity project (no Assets/ folder)." >&2
  echo "    Run ./setup_vermin.sh first." >&2
  exit 1
fi

echo "[+] Unity editor:   $UNITY_EXE"
echo "[+] Project path:   $PROJECT_PATH"

# -------- modes --------
case "${1:-open}" in
  open)
    exec "$UNITY_EXE" -projectPath "$PROJECT_PATH"
    ;;

  --setup|setup)
    # Open and auto-run the setup method
    exec "$UNITY_EXE" \
      -projectPath "$PROJECT_PATH" \
      -executeMethod Vermin.Editor.VerminProjectSetup.RunAllFromCli
    ;;

  --build-android|build-android)
    # Headless Android build
    LOGFILE="$PROJECT_PATH/Logs/android_build_$(date +%Y%m%d_%H%M%S).log"
    mkdir -p "$PROJECT_PATH/Logs"
    echo "[+] Build log: $LOGFILE"
    exec "$UNITY_EXE" \
      -projectPath "$PROJECT_PATH" \
      -batchmode -nographics -quit \
      -buildTarget Android \
      -executeMethod Vermin.Editor.VerminProjectSetup.BuildAndroidFromCli \
      -logFile "$LOGFILE"
    ;;

  -h|--help|help)
    sed -n '1,15p' "$0"
    exit 0
    ;;

  *)
    echo "Unknown mode: $1 (use: open | --setup | --build-android)" >&2
    exit 1
    ;;
esac
