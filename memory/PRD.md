# Vermin Exterminator — Phase: Upgrade Shop UI + Optics Visual Polish + QA

## Original Problem Statement
Unity 2023 LTS + URP Android first-person sniper "Vermin Exterminator" (aka
Vermin Infestation). Extend the existing project (cloned from
https://github.com/Ch405-L9/Vermin-Exterminator.git) with three phases:
1. Full Upgrade Shop UI + loadout UX on top of existing
   `WeaponPartCatalog` / `PlayerProgress` / `CurrencyManager` / early
   `UpgradeShopController`.
2. Optics Visual Polish (URP Volumes + scope overlay + rat eye-shine) across
   Daylight / NightVision / Thermal WhiteHot / Thermal Green modes with
   tiered optics.
3. QA/safety — compile cleanliness, null-safe fallbacks, Android build target
   compatibility; document everything in `PHASE2-NOTES.md`.

Constraints: additive only, preserve existing systems, keep namespace layout
(`BarnSwarmSniper.*`), no runtime mutation of ScriptableObjects.

## User Decisions
- Code-only changes + inspector-wiring notes in `PHASE2-NOTES.md` (Unity
  Editor not available in this environment; user validates in local Unity).
- Commit locally in `/app/vermin` only (no push).
- Priority: Phase 1 is must-have; Phases 2 & 3 also delivered.
- UI framework: UGUI (Canvas + TMP), not UI Toolkit.
- Art assets: wire slots only; user will provide final art.

## Architecture
- Data: `PlayerProgress` persisted JSON in `Application.persistentDataPath`.
- Weapon: `WeaponPartCatalog` (SO) → `RuntimeLoadout.Build` (per-mission
  snapshot) → `WeaponController.ApplyModifiers` → recoil/fire-rate/aim-assist.
- Optics: `OpticsTierConfig` (SO) with per-tier supported scope modes, sway
  mult, aim-assist cone mult; `ScopeOverlayController` swaps URP
  `VolumeProfile` via `ScopeVolumeProfileSet`; `RatEyeShine` listens to the
  static `OnScopeModeChanged` event.
- UI: `MainMenuController` routes Main / Settings / Shop panels;
  `UpgradeShopController` dynamically builds category tabs and item rows
  from the catalog + progress; per-row `ShopItemButton` prefab.

## What's Been Implemented (2026-04-18)
Phase 1 — Upgrade Shop UI & Loadout
- Extended `WeaponPartCatalog` with `Rifle/Ammo/Cosmetic` categories,
  `WeaponPartRarity`, icon / description / requiresContractIds /
  opticsTierIndex per part, and `zoomMultiplier / swayMultiplier /
  noiseRadiusMultiplier / magazineSizeDelta` on modifiers.
- Extended `PlayerProgress` with `seenPartIds`, `completedContractIds`,
  `selectedAmmoId` (JsonUtility-safe defaults, backward-compatible).
- `RuntimeLoadout` (new) — immutable per-mission snapshot.
- Full Upgrade Shop (`UpgradeShopController` rewritten) — dynamic category
  tabs, item list, detail panel with current-vs-with-part stat preview,
  buy/equip with human-readable errors, instant pellet refresh, NEW badge.
- `ShopItemButton` (new) for the item row prefab.
- `MainMenuController` — Shop button/panel wiring, null-safe.
- `GameManager` — builds `CurrentRuntimeLoadout` before each mission,
  records completed contract IDs, exposes `WeaponPartCatalog`.

Phase 2 — Optics Visual Polish (URP)
- `ScopeVolumeProfileSet` (new) SO — maps `ScopeMode → VolumeProfile` +
  reticle sprite/color + HUD accent + rat eye-shine intensity/color.
- `OpticsTierConfig` extended — per-tier `supportedScopeModes`, min/max FOV,
  swayMultiplier, aimAssistConeMultiplier; `TryGetTier` / `SupportsMode`.
- `ScopeOverlayController` rewritten — Volume swapping, per-mode reticle
  + HUD accent, static `OnScopeModeChanged` event, `CycleMode`,
  `EnforceSupportedMode`.
- `RatEyeShine` (new) — per-rat emissive driver.
- `WeaponController` — per-tier sway/aim-assist applied on top of
  equipped-part modifiers, aim-assist base captured once, `CycleScopeMode`
  helper.

Phase 3 — QA/Docs
- `PHASE2-NOTES.md` at repo root — new/changed scripts, required inspector
  wiring (MainMenu / Game scene / prefabs / URP Volume profiles), TODOs.

## Compliance with Global Rules
- No existing system deleted or rewritten beyond surgical extension.
- All changes backward-compatible with existing `.asset` serialization
  (enum values appended, no renames).
- Namespaces preserved (`BarnSwarmSniper.{Data, Weapon, UI, Game, AI, ...}`).
- Compiles against C# 10 / Unity 2023 LTS.
- ScriptableObjects are read, never mutated at runtime.
- Narrative (marine sniper → vermin-control) untouched.

## Pending / Backlog
P1
- Wire real art for shop icons / NEW badge / rarity borders / reticles.
- Hook `WeaponController.CycleScopeMode()` to a UI button or gesture.
- In-game shop return path (swap between MainMenu & shop seamlessly).

P2
- Magazine/reload system (consume `magazineSizeDelta`).
- Noise-alert system for rats (consume `noiseRadiusMultiplier`).
- Headshot telemetry for contract challenges (pre-existing TODO).
- Cosmetic-only parts: exclude from stat aggregation when cosmetic pipeline
  lands.

## Validation Status
- Unity Editor not available in this environment — code-only delivery.
- Brace balance checked across every new/modified file.
- Namespace / reference consistency verified by grep.
- User to validate build/compile in Unity 2023 LTS locally.
- See "Suggested in-editor smoke checks" in `PHASE2-NOTES.md` for QA steps.

## Delivery Location
All changes committed locally to `/app/vermin` on `main` branch:
- Commit: `c5e0882 Phase: Upgrade Shop UI + Optics Visual Polish + QA`
- 14 files changed, 1360 insertions, 191 deletions.
- 5 new files: `PHASE2-NOTES.md`, `RatEyeShine.cs`, `RuntimeLoadout.cs`,
  `ScopeVolumeProfileSet.cs`, `ShopItemButton.cs`.
