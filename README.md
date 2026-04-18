# Vermin Infestation (Android)

`Vermin Infestation` is a mobile sniper shooter built with Unity URP where the player clears rat waves across generated barn layouts using scoped aim, recoil, and score-driven progression.

## Requirements

- Unity `2023 LTS`
- Universal Render Pipeline (URP) package enabled
- Android Build Support module installed (SDK/NDK/OpenJDK via Unity Hub)

## Open Project

1. Open Unity Hub.
2. Add this folder as a Unity project.
3. Open with Unity 2023 LTS.
4. Ensure scenes exist and are added to Build Settings:
   - `Assets/Scenes/MainMenu.unity`
   - `Assets/Scenes/Game.unity`

## Android Build Setup

1. Open `File > Build Settings`.
2. Select `Android` and click `Switch Platform`.
3. Open `Player Settings` and configure:
   - `Company Name` / `Product Name` (`Vermin Infestation`)
   - `Package Name` (e.g. `com.yourstudio.vermininfestation`)
   - `Orientation`: Portrait or Landscape (pick one and keep UI aligned)
   - `Scripting Backend`: `IL2CPP`
   - `Target Architectures`: `ARM64` (and optionally ARMv7)
   - `Internet Access`: as required by your analytics stack
4. In Build Settings, choose `Development Build` for debug APK.
5. Click `Build` and output APK to a local folder.

## System Flow

`Input (gyro + touch)` -> `Camera/Scope` -> `WeaponController` -> `RatAIManager` -> `Score/Currency` -> `HUD/MainMenu`

## Runtime Architecture

- `GameManager` is the bootstrap entrypoint and state machine:
  `MainMenu -> Loading -> Playing -> Paused -> Results`.
- `PlayerProgressManager` and `SettingsManager` persist data in `Application.persistentDataPath`.
- `LevelGenerator` + `SpawnZoneManager` configure level content and rat spawn flow.
- `RatObjectPool` and `EffectsObjectPool` keep allocation churn low.

## Mobile Performance Notes

- Use object pooling for rats and hit effects (already in project).
- Keep rat cap conservative (`RatAIManager._maxActiveRats`) for mid-range devices.
- Avoid expensive per-frame allocations in UI and AI loops.
- Use URP mobile quality profiles and limit dynamic lights/shadows.
- Profile on-device with Unity Profiler before increasing spawn density.
