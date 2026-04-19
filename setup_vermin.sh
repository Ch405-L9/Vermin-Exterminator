#!/usr/bin/env bash
# setup_vermin.sh - pre-Unity repo setup. Run once.
set -euo pipefail

REPO_ROOT="$(pwd)"
URP_SOURCE=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --urp-source) URP_SOURCE="$2"; shift 2;;
    -h|--help) echo "Usage: ./setup_vermin.sh [--urp-source /path/to/temp/urp/project]"; exit 0;;
    *) echo "Unknown arg: $1" >&2; exit 1;;
  esac
done

[[ -d ".git" ]] || { echo "[x] Run from repo root"; exit 1; }

echo "== Vermin Exterminator setup at $REPO_ROOT =="

# URP scaffolding
if [[ -d "Assets" && -d "Packages" && -d "ProjectSettings" ]]; then
  echo "[=] Unity scaffolding already present - skipping."
else
  if [[ -n "$URP_SOURCE" ]]; then
    [[ -d "$URP_SOURCE/Assets" ]] || { echo "[x] --urp-source invalid"; exit 1; }
    echo "[+] Copying URP scaffolding from $URP_SOURCE"
    cp -r "$URP_SOURCE/Assets" .
    cp -r "$URP_SOURCE/Packages" .
    cp -r "$URP_SOURCE/ProjectSettings" .
  else
    echo "[+] Creating minimal scaffolding (Unity will install URP on first open)"
    mkdir -p Assets Packages ProjectSettings
    cat > Packages/manifest.json <<'JSON'
{
  "dependencies": {
    "com.unity.render-pipelines.universal": "16.0.6",
    "com.unity.textmeshpro": "3.2.0-pre.10",
    "com.unity.inputsystem": "1.11.2",
    "com.unity.ugui": "2.0.0",
    "com.unity.ide.visualstudio": "2.0.22",
    "com.unity.test-framework": "1.4.5",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.video": "1.0.0"
  }
}
JSON
    cat > ProjectSettings/ProjectVersion.txt <<'TXT'
m_EditorVersion: 2023.2.20f1
m_EditorVersionWithRevision: 2023.2.20f1 (0e25a174756c)
TXT
    echo "[!] Minimal scaffolding. For full fidelity, rerun with --urp-source."
  fi
fi

echo "[+] Creating Assets/Scripts/<area> folders"
mkdir -p Assets/Scripts/{AI,Camera,Data,Game,Input,Level,Pooling,Scoring,UI,Weapon}
mkdir -p Assets/Editor Assets/Scenes Assets/Data/Contracts Assets/Settings/Volumes Assets/Prefabs/UI

move_if_exists() {
  local dest="$1"; shift
  for f in "$@"; do
    [[ -f "$f" ]] && mv "$f" "$dest/" && echo "    $f -> $dest/"
  done
}

echo "[+] Moving .cs files into Assets/Scripts/"
move_if_exists Assets/Scripts/AI       RatFSM.cs RatAIManager.cs MovementPathfinder.cs ScatterBehavior.cs SpawnZone.cs SpawnZoneManager.cs RatEyeShine.cs
move_if_exists Assets/Scripts/Camera   SniperCameraController.cs ZoomController.cs BreathingSway.cs
move_if_exists Assets/Scripts/Data     PlayerProgress.cs PlayerProgressManager.cs PlayerProgressUpgrades.cs SettingsData.cs SettingsManager.cs RuntimeSettings.cs RuntimeLoadout.cs ContractDefinition.cs ContractChallenge.cs ContractSet.cs
move_if_exists Assets/Scripts/Game     GameManager.cs ContractManager.cs
move_if_exists Assets/Scripts/Input    GyroInputHandler.cs TouchInputHandler.cs HybridInputController.cs InputSmoothingPipeline.cs
move_if_exists Assets/Scripts/Level    LevelGenerator.cs LevelSeedGenerator.cs EnvironmentTileDescriptor.cs EnvironmentTileLibrary.cs LightingModeController.cs
move_if_exists Assets/Scripts/Pooling  RatObjectPool.cs EffectsObjectPool.cs
move_if_exists Assets/Scripts/Scoring  ScoreManager.cs ComboTracker.cs CurrencyManager.cs
move_if_exists Assets/Scripts/UI       MainMenuController.cs HUDController.cs ScopeOverlayController.cs UpgradeShopController.cs ShopItemButton.cs HitMarkerFeedback.cs
move_if_exists Assets/Scripts/Weapon   WeaponController.cs WeaponPartCatalog.cs OpticsTierConfig.cs RecoilSystem.cs ScopeVolumeProfileSet.cs

LEFTOVER=$(ls *.cs 2>/dev/null | wc -l)
if [[ "$LEFTOVER" != "0" ]]; then
  echo "[!] $LEFTOVER .cs files at root -> Assets/Scripts/_Unsorted/"
  mkdir -p Assets/Scripts/_Unsorted
  mv *.cs Assets/Scripts/_Unsorted/ 2>/dev/null || true
fi

echo "[+] Moving docs to Docs/"
mkdir -p Docs Docs/screenshots
for f in PHASE2-NOTES.md URP_SETUP_NOTES.md SceneSetupInstructions.md InspectorContractChecklist.md; do
  [[ -f "$f" ]] && mv "$f" Docs/ && echo "    $f -> Docs/"
done
shopt -s nullglob
for f in 2026-*.png 03-*.png; do mv "$f" Docs/screenshots/ 2>/dev/null || true; done
[[ -d "video_thumbs" ]] && mv video_thumbs Docs/ 2>/dev/null || true
shopt -u nullglob

echo "[+] Cleaning cruft"
rm -f "SYSTEM ROLE EXECUTION.txt" "SYSTEMROLEEXECUTION.txt" "SHN.json" "manager.txt" "VERMIN-HUNTER"

echo "[+] Updating .gitignore"
touch .gitignore
add_ignore() { grep -qxF "$1" .gitignore || echo "$1" >> .gitignore; }
add_ignore ""
add_ignore "# Unity generated"
for pat in "[Ll]ibrary/" "[Tt]emp/" "[Oo]bj/" "[Bb]uild/" "[Bb]uilds/" "[Ll]ogs/" "[Uu]ser[Ss]ettings/" "*.csproj" "*.sln" "*.user" "*.suo" "*.apk" "*.aab" ".vs/" ".vscode/" ".idea/"; do
  add_ignore "$pat"
done

echo ""
echo "== Setup complete =="
echo "Next: ./open_vermin.sh   (then Vermin -> Setup All in Unity)"
echo "Scripts: $(find Assets/Scripts -name '*.cs' 2>/dev/null | wc -l) files"
