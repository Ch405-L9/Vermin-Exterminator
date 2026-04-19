# Inspector Contract Checklist (Scene Wiring)

Goal: wire scenes without guesswork. Every field listed below is either `[SerializeField]` or a required reference implied by the script’s runtime checks.

## Global / Persistent (MainMenu scene `Bootstrap` GameObject)

### `GameManager` (on `Bootstrap`)
- `_playerProgressManager` -> `Bootstrap/PlayerProgressManager`
- `_settingsManager` -> `Bootstrap/SettingsManager`
- `_scoreManager` -> `Bootstrap/ScoreManager`
- `_currencyManager` -> `Bootstrap/CurrencyManager`
- Optional (auto-resolved in Game scene if left blank):
  - `_levelGenerator` -> `RuntimeManagers/LevelGenerator`
  - `_spawnZoneManager` -> `RuntimeManagers/SpawnZoneManager`
  - `_ratAIManager` -> `RuntimeManagers/RatAIManager`
  - `_weaponController` -> `Weapon/WeaponController`
  - `_hudController` -> `HUD Canvas/HUDController`

### `PlayerProgressManager` (on `Bootstrap`)
- No inspector fields.

### `SettingsManager` (on `Bootstrap`)
- No inspector fields.

### `ScoreManager` (on `Bootstrap`)
- No inspector fields.

### `CurrencyManager` (on `Bootstrap`)
- No inspector fields.

## Main Menu UI (MainMenu scene)

### `MainMenuController` (on `MainMenuController` GameObject)
- `_playButton` -> `Canvas_MainMenu/Panel_MainMenu/PlayButton (Button)`
- `_settingsButton` -> `Canvas_MainMenu/Panel_MainMenu/SettingsButton (Button)`
- `_closeSettingsButton` -> `Canvas_MainMenu/Panel_Settings/CloseSettingsButton (Button)` (optional)
- `_exitButton` -> `Canvas_MainMenu/Panel_MainMenu/ExitButton (Button)`
- `_currentLevelText` -> `Canvas_MainMenu/Panel_MainMenu/CurrentLevelText (TMP_Text)`
- `_pelletsText` -> `Canvas_MainMenu/Panel_MainMenu/PelletsText (TMP_Text)`
- `_opticsTierText` -> `Canvas_MainMenu/Panel_MainMenu/OpticsTierText (TMP_Text)`
- `_mainMenuPanel` -> `Canvas_MainMenu/Panel_MainMenu (GameObject)`
- `_settingsPanel` -> `Canvas_MainMenu/Panel_Settings (GameObject)`
- `_breathingSwayToggle` -> `Canvas_MainMenu/Panel_Settings/BreathingSwayToggle (Toggle)` (optional)
  - If assigned, hook toggle event to call `MainMenuController.OnBreathingSwayToggleChanged(bool)`

## Gameplay Core (Game scene)

### `RuntimeManagers/LevelGenerator` (`LevelGenerator`)
- `_tileLibrary` -> `EnvironmentTileLibrary (ScriptableObject asset)`
- `_spawnZoneManager` -> `RuntimeManagers/SpawnZoneManager`
- `_lightingModeController` -> `RuntimeManagers/LightingModeController`
- `_levelSeedGenerator` -> `RuntimeManagers/LevelSeedGenerator`
- `_ratConfigs` -> configured in inspector (array)

### `RuntimeManagers/SpawnZoneManager` (`SpawnZoneManager`)
- `_ratAIManager` -> `RuntimeManagers/RatAIManager`
- `_maxActiveRatsPerZone` -> tune
- `_spawnInterval` -> tune
- Runtime requirement: call `ConfigureSpawnZones(Transform[])` with real zone transforms (your tile prefab should expose them or you create zone markers under the tile).

### `RuntimeManagers/RatAIManager` (`RatAIManager`)
- `_ratObjectPool` -> `RatPool/RatObjectPool`
- `_maxActiveRats` -> tune

### `RatPool/RatObjectPool` (`RatObjectPool`)
- `_ratPrefab` -> Rat prefab (must include `RatFSM` + collider + (recommended) `CharacterController`)
- `_poolSize` -> tune

### `EffectsPool/EffectsObjectPool` (`EffectsObjectPool`)
- `_hitMarkerPrefab` -> hit marker prefab (pooled)
- `_impactDustPrefab` -> impact dust prefab (pooled)
- `_poolSize` -> tune

### `RuntimeManagers/LightingModeController` (`LightingModeController`)
- `_globalPostProcessVolume` -> `Global Volume (URP Volume component)` with a Profile
- `_daylightAmbientColor` -> tune
- `_nightVisionAmbientColor` -> tune
- `_thermalAmbientColor` -> tune

### `RuntimeManagers/LevelSeedGenerator` (`LevelSeedGenerator`)
- No inspector fields.

## Player / Camera / Weapon (Game scene)

### `Input/InputSmoothingPipeline` (`InputSmoothingPipeline`)
- `_gyroSmoothingFactor` -> tune (0..1)
- `_touchSmoothingFactor` -> tune (0..1)

### `Input/GyroInputHandler` (`GyroInputHandler`)
- `_settingsData` -> `SettingsData (ScriptableObject asset)`
- `_maxAngularVelocity` -> tune

### `Input/TouchInputHandler` (`TouchInputHandler`)
- `_settingsData` -> `SettingsData (ScriptableObject asset)`
- `_rightSideThreshold` -> tune

### `Input/HybridInputController` (`HybridInputController`)
- `_gyroInputHandler` -> `Input/GyroInputHandler`
- `_touchInputHandler` -> `Input/TouchInputHandler`
- `_smoothingPipeline` -> `Input/InputSmoothingPipeline`
- `_settingsData` -> `SettingsData (ScriptableObject asset)`
- `_weaponController` -> `Weapon/WeaponController`
- `_gyroDeadZone` -> tune

### `Main Camera/SniperCameraController` (`SniperCameraController`)
- `_inputController` -> `Input/HybridInputController`
- `_cameraRoot` -> a child transform that you rotate (often the camera pivot)
- `_recoilSystem` -> `Weapon/RecoilSystem` (or wherever you place it)

### `Main Camera/ZoomController` (`ZoomController`)
- `_mainCamera` -> `Main Camera (Camera component)` (can be left blank if on same GO)
- `_opticsConfig` -> `OpticsTierConfig (ScriptableObject asset)`

### `Main Camera/BreathingSway` (`BreathingSway`) (optional)
- `_swayMagnitude` -> tune
- `_swaySpeed` -> tune
- `_enableSway` -> should follow Settings (toggle via SettingsManager + your UI hook)

### `Weapon/WeaponController` (`WeaponController`)
- `_cameraController` -> `Main Camera/SniperCameraController`
- `_zoomController` -> `Main Camera/ZoomController`
- `_opticsConfig` -> `OpticsTierConfig (ScriptableObject asset)`
- `_recoilSystem` -> `Weapon/RecoilSystem`
- `_ratAIManager` -> `RuntimeManagers/RatAIManager`
- `_scoreManager` -> `Bootstrap/ScoreManager` (persisted from MainMenu)
- `_effectsObjectPool` -> `EffectsPool/EffectsObjectPool`
- `_settingsData` -> `SettingsData (ScriptableObject asset)`
- `_fireRate` -> tune
- `_ratLayer` -> LayerMask configured to match rat colliders
- `_shootSound` -> AudioClip (optional)
- `_audioSource` -> AudioSource on same GO (optional)

### `Weapon/RecoilSystem` (`RecoilSystem`)
- `_recoilAmount` -> tune
- `_recoilSpeed` -> tune
- `_recoverySpeed` -> tune

## HUD / Scope UI (Game scene)

### `HUD Canvas/HUDController` (`HUDController`)
- `_scoreText` -> TMP score label
- `_comboText` -> TMP combo label (optional)
- `_timerText` -> TMP timer label
- `_pelletsText` -> TMP pellets label (optional; GameManager publishes pellet events)

### `Scope Canvas/ScopeOverlayController` (`ScopeOverlayController`) (if used)
- `_vignetteImage` -> UI Image (scope vignette)
- `_reticleImage` -> UI Image (reticle/crosshair overlay)
- `_zoomText` -> TMP zoom label
- `_modeText` -> TMP mode label (Day/NV/Thermal…)
- `_timerText` -> TMP timer label (optional duplicate of HUD timer)
- `_killCountText` -> TMP kills label
- `_recIcon` -> GameObject for REC indicator (optional)

### `HUD Canvas/HitMarkerFeedback` (`HitMarkerFeedback`) (optional)
- `_effectsObjectPool` -> `EffectsPool/EffectsObjectPool`
- `_displayDuration` -> tune

## Asset Contracts (must exist as assets)

- `SettingsData` (ScriptableObject): must exist and be assigned anywhere requested.
- `OpticsTierConfig` (ScriptableObject): must exist and be assigned to `ZoomController` and optionally `WeaponController`.
- `EnvironmentTileLibrary` (ScriptableObject): must exist and be assigned to `LevelGenerator`.

## Two “gotchas” to resolve early

1. **Spawn zones are currently configured with an empty array** in `LevelGenerator.GenerateLevel` — you must provide real spawn zone transforms (either on the tile prefab or via a `SpawnZonesRoot` object you pass to `ConfigureSpawnZones`).
2. **Rat prefab must include** `RatFSM` (required by pool), and should include collider + (recommended) `CharacterController` to match `RatFSM` movement code.

