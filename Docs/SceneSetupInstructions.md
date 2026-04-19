# Scene Setup Instructions - Vermin Infestation

This guide wires the existing scripts without changing architecture.

## `Assets/Scenes/MainMenu.unity`

1. Create scene and save as `Assets/Scenes/MainMenu.unity`.
2. Create root `Bootstrap` GameObject and attach:
   - `GameManager`
   - `PlayerProgressManager`
   - `SettingsManager`
   - `ScoreManager`
   - `CurrencyManager`
3. In `GameManager` inspector, assign:
   - `_playerProgressManager` -> `Bootstrap` / `PlayerProgressManager`
   - `_settingsManager` -> `Bootstrap` / `SettingsManager`
   - `_scoreManager` -> `Bootstrap` / `ScoreManager`
   - `_currencyManager` -> `Bootstrap` / `CurrencyManager`
   - Keep gameplay refs empty in this scene (auto-resolved in `Game` scene)
4. Create `Canvas_MainMenu` (Screen Space - Overlay, Scale With Screen Size).
5. Under `Canvas_MainMenu`, create:
   - `Panel_MainMenu`
   - `Panel_Settings` (disabled by default)
6. Add text fields (TextMeshProUGUI) under `Panel_MainMenu`:
   - `CurrentLevelText`
   - `PelletsText`
   - `OpticsTierText`
7. Add buttons:
   - `PlayButton`
   - `SettingsButton`
   - `ExitButton`
   - Optional in settings panel: `CloseSettingsButton`
8. Create `MainMenuController` GameObject and attach `MainMenuController`.
9. Wire `MainMenuController` serialized fields:
   - `_playButton` -> `PlayButton`
   - `_settingsButton` -> `SettingsButton`
   - `_closeSettingsButton` -> `CloseSettingsButton` (optional)
   - `_exitButton` -> `ExitButton`
   - `_currentLevelText` -> `CurrentLevelText`
   - `_pelletsText` -> `PelletsText`
   - `_opticsTierText` -> `OpticsTierText`
   - `_mainMenuPanel` -> `Panel_MainMenu`
   - `_settingsPanel` -> `Panel_Settings`
   - `_breathingSwayToggle` -> settings toggle (optional)
10. Add `MainMenu` to Build Settings scenes list.

## `Assets/Scenes/Game.unity`

1. Create scene and save as `Assets/Scenes/Game.unity`.
2. Add an empty `RuntimeManagers` object and place these existing components:
   - `LevelGenerator`
   - `SpawnZoneManager`
   - `RatAIManager`
   - `LightingModeController`
   - `LevelSeedGenerator`
3. Add pooling objects:
   - `RatPool` with `RatObjectPool`
   - `EffectsPool` with `EffectsObjectPool`
4. Add gameplay objects:
   - `PlayerRig`
   - `Main Camera` with `SniperCameraController`
   - `Weapon` with `WeaponController`
   - Input object with `GyroInputHandler`, `TouchInputHandler`, `InputSmoothingPipeline`, `HybridInputController`
5. Add UI canvas and HUD object with `HUDController`.
6. Wire AI and spawning:
   - `RatAIManager._ratObjectPool` -> `RatPool/RatObjectPool`
   - `SpawnZoneManager._ratAIManager` -> `RatAIManager`
   - `LevelGenerator._spawnZoneManager` -> `SpawnZoneManager`
   - `LevelGenerator._lightingModeController` -> `LightingModeController`
   - `LevelGenerator._levelSeedGenerator` -> `LevelSeedGenerator`
7. Wire weapon:
   - `WeaponController._cameraController` -> `Main Camera/SniperCameraController`
   - `WeaponController._zoomController` -> `ZoomController`
   - `WeaponController._recoilSystem` -> `RecoilSystem`
   - `WeaponController._ratAIManager` -> `RatAIManager`
   - `WeaponController._scoreManager` -> `Bootstrap/ScoreManager` (persisted via `DontDestroyOnLoad`)
   - `WeaponController._effectsObjectPool` -> `EffectsPool/EffectsObjectPool`
8. Wire input:
   - `HybridInputController._gyroInputHandler` -> `GyroInputHandler`
   - `HybridInputController._touchInputHandler` -> `TouchInputHandler`
   - `HybridInputController._smoothingPipeline` -> `InputSmoothingPipeline`
   - `HybridInputController._weaponController` -> `WeaponController`
9. In `GameManager` (persisted from MainMenu), assign gameplay refs after entering Play once:
   - `_levelGenerator`, `_spawnZoneManager`, `_ratAIManager`, `_weaponController`, `_hudController`
   - Or leave blank and rely on auto-resolve by type.
10. Add `Game` to Build Settings scenes list after `MainMenu`.
