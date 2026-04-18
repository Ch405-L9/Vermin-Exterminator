# PHASE2 – Upgrade Shop UI + Optics Visual Polish + QA

Scope of this phase: add a fully-functional Upgrade Shop UI, a per-scope-mode
URP post-processing layer, and the supporting data plumbing. The core
gameplay loop, input, FSM, pooling, contracts and level generation were **not
modified** beyond additive hooks.

All code compiles against Unity 2023 LTS + URP. No `.asset`, `.prefab` or
`.unity` scene files were edited in this pod (no Unity Editor available); the
notes below enumerate every inspector field that needs wiring.

---

## NEW / CHANGED SCRIPTS

### New

| File | Purpose |
|---|---|
| `RuntimeLoadout.cs` | Immutable snapshot: equipped parts + selected ammo + aggregated `WeaponStatModifiers`. Built by `GameManager` before a mission. |
| `ShopItemButton.cs` | Per-item UI row. Lives on the shop's item-list prefab. |
| `ScopeVolumeProfileSet.cs` | ScriptableObject mapping each `ScopeMode` → `VolumeProfile` + reticle sprite/color + HUD accent + rat eye-shine color/intensity. |
| `RatEyeShine.cs` | Per-rat emissive driver. Subscribes to `ScopeOverlayController.OnScopeModeChanged`. |

### Changed (extended, **backward compatible**)

| File | Changes |
|---|---|
| `WeaponPartCatalog.cs` | Added `Rifle / Ammo / Cosmetic` to `WeaponPartCategory` (appended — existing serialized indices preserved). Added `WeaponPartRarity` enum. Added `description`, `icon`, `rarity`, `requiresContractIds`, `opticsTierIndex` to `WeaponPartDefinition`. Extended `WeaponStatModifiers` with `zoomMultiplier`, `swayMultiplier`, `noiseRadiusMultiplier`, `magazineSizeDelta`. Added `GetPopulatedCategories()` and `GetShortStatLine()`. |
| `PlayerProgress.cs` | Added `seenPartIds[]` (NEW-badge tracking), `completedContractIds[]` (contract prerequisite gating), `selectedAmmoId`. |
| `PlayerProgressUpgrades.cs` | Added `HasSeenPart / MarkPartSeen`, `HasCompletedContract / MarkContractCompleted`, `GetSelectedAmmoId / SetSelectedAmmoId` extension methods. |
| `UpgradeShopController.cs` | **Rewritten.** Full shop: dynamic category tabs, item list, stat preview (current vs with part), buy/equip with human-readable errors, auto-refresh on `CurrencyManager.OnPelletsChanged`, NEW badges. Keeps public `TryBuy` / `TryEquip` / `SelectPart` API for existing callers. |
| `MainMenuController.cs` | Added `_shopButton`, `_closeShopButton`, `_shopPanel`, `ShowShopMenu()`. All existing button references are now null-safe. |
| `GameManager.cs` | Builds and exposes `CurrentRuntimeLoadout` before each mission. Records `SelectedContract.contractId` into `PlayerProgress.completedContractIds` on successful end. Exposes `WeaponPartCatalog` publicly. No runtime mutation of ScriptableObjects. |
| `OpticsTierConfig.cs` | `OpticsTier` now has `description`, `minFieldOfView / maxFieldOfView`, `swayMultiplier`, `aimAssistConeMultiplier`, and a `supportedScopeModes` list. Added `SupportsMode()` and `TryGetTier()`. Legacy `zoomLevel / FieldOfView / defaultScopeMode` preserved. |
| `ScopeOverlayController.cs` | Wires URP Volume switching via `ScopeVolumeProfileSet`, per-mode reticle sprite + HUD accent tinting. Exposes `CurrentScopeMode` + static `OnScopeModeChanged` event. Adds `CycleMode(allowedModes)` and `EnforceSupportedMode(tier)`. |
| `WeaponController.cs` | Applies per-tier `swayMultiplier` / `aimAssistConeMultiplier` on top of equipped-part modifiers. Captures base aim-assist values once (so repeat `ApplyModifiers` calls don't compound). Adds `CycleScopeMode()` helper. Optionally holds `ScopeOverlayController` ref to clamp mode to supported modes when tier changes. |

No files were deleted.

---

## PHASE 1 – UPGRADE SHOP : REQUIRED INSPECTOR WIRING

### MainMenu scene

1. **MainMenu Canvas → Main Menu Panel**
   - Add a new `Button_Shop` button.
   - Create a new root child `ShopPanel` (SetActive=false by default).
2. **ShopPanel layout** (UGUI):
   ```
   ShopPanel (GameObject with UpgradeShopController component)
   ├── Header
   │   ├── Label_Pellets (TMP)            → UpgradeShopController._pelletsText
   │   ├── Label_Status  (TMP)            → UpgradeShopController._statusText
   │   └── Button_CloseShop               → MainMenuController._closeShopButton
   ├── CategoryTabs (HorizontalLayoutGroup)→ UpgradeShopController._categoryTabsContainer
   │   └── (empty — filled at runtime)
   ├── ItemList (VerticalLayoutGroup inside ScrollView Content)
   │                                      → UpgradeShopController._itemListContainer
   └── DetailPanel
       ├── Image_DetailIcon               → UpgradeShopController._detailIcon
       ├── Label_DetailName (TMP)         → UpgradeShopController._detailName
       ├── Label_DetailRarity (TMP)       → UpgradeShopController._detailRarity
       ├── Label_DetailDescription (TMP)  → UpgradeShopController._detailDescription
       ├── Label_DetailReqs (TMP)         → UpgradeShopController._detailRequirements
       ├── Label_StatsCurrent (TMP)       → UpgradeShopController._detailStatsCurrent
       ├── Label_StatsWith (TMP)          → UpgradeShopController._detailStatsWith
       ├── Button_Buy → has child TMP     → UpgradeShopController._buyButton / _buyButtonLabel
       └── Button_Equip → has child TMP   → UpgradeShopController._equipButton / _equipButtonLabel
   ```
3. **Prefabs** (drop into `Assets/Prefabs/UI/`):
   - `CategoryTabButton.prefab` — a Button with a child TMP text.
     → UpgradeShopController._categoryTabPrefab
   - `ShopItemRow.prefab` — a Button with:
     - `Image_Icon`, `Label_Name`, `Label_Cost`, `Label_Rarity`, `Label_ShortStats`,
       `Lock_Icon` (GO), `Owned_Badge` (GO), `Equipped_Badge` (GO), `New_Badge` (GO).
     - `ShopItemButton` component with all fields wired.
     → UpgradeShopController._itemButtonPrefab

4. **MainMenuController** wiring additions:
   - `_shopButton` ← `Button_Shop`
   - `_closeShopButton` ← `Button_CloseShop`
   - `_shopPanel` ← `ShopPanel`

5. **WeaponPartCatalog.asset** (existing ScriptableObject):
   - Populate `parts` with entries per category.
   - Required per entry: `id`, `displayName`, `category`, `costPellets`, `requiredPlayerLevel`.
   - Optional: `description`, `icon`, `rarity`, `requiresPartIds`, `requiresContractIds`, `modifiers.*`.
   - For **Scope** parts: set `opticsTierIndex` to match the intended `OpticsTierConfig.OpticsTiers[i]`.

6. **GameManager prefab / scene object**:
   - Wire `_weaponPartCatalog` to the populated catalog.
   - Everything else stays as-is.

> **UX expectations met**: instant pellet refresh (subscribed to `CurrencyManager.OnPelletsChanged`), "Not enough pellets" / "Requires level N" / "Requires <prereq>" / "Already owned" messages, one-part-per-category enforced by `PlayerProgress.EquipPart`, NEW badge cleared on first view via `MarkPartSeen`, `RuntimeLoadout` built before each mission and consumed by `WeaponController.ApplyModifiers`.

---

## PHASE 2 – OPTICS URP : REQUIRED INSPECTOR WIRING

### URP Volume profiles (create four assets)

Under `Assets/Settings/Volumes/`:

| Asset | Key overrides (suggested) |
|---|---|
| `Volume_Daylight.asset` | neutral Color Adjustments, slight Bloom, light Vignette |
| `Volume_NightVision.asset` | Color Adjustments (saturation ≈ −100, postExposure ≈ −1, colorFilter ≈ (0.1, 1.0, 0.2)); Bloom intensity 1.5; Vignette 0.45; Film Grain 0.5 |
| `Volume_ThermalWhiteHot.asset` | Color Adjustments saturation −100; Channel Mixer / LUT for white-hot; strong Vignette 0.55; Film Grain 0.1 |
| `Volume_ThermalGreen.asset` | Color Adjustments saturation −100, colorFilter (0.3, 1.0, 0.5); Bloom 2.0; Vignette 0.45 |

### Scope volume profile set

Create `ScopeVolumeProfileSet.asset`
(`Assets/Create → BarnSwarmSniper → Scope Volume Profile Set`).
Add 4 entries, one per `ScopeMode`, each with:
- `profile`  → matching VolumeProfile above
- `reticleSprite` → (TBD art; placeholder sprite OK)
- `reticleColor`, `hudAccentColor` — mode-appropriate
- `ratEyeShineColor`, `ratEyeShineIntensity` — red for Daylight, green-hot for NV (~3), white-hot for Thermal (~4)

### Scene wiring (Game scene)

1. **Global Scope Volume** GameObject
   - Add `Volume` component (set `Is Global = true`, `Priority = 10`, `Weight = 1`).
   - Leave `Profile` empty — `ScopeOverlayController` will drive it.
   - Drag this Volume into `ScopeOverlayController._globalScopeVolume`.
2. **ScopeOverlayController** (on the scope HUD canvas)
   - `_profileSet` ← the ScriptableObject above.
   - `_reticleImage`, `_modeText`, etc. — already used.
   - `_hudAccentTargets` — array of Graphics that should tint per mode (reticle, vignette ring, mode label, HUD frame icons).
   - `_defaultScopeMode` — `Daylight`.
3. **Rat prefab**
   - Add `RatEyeShine` component.
   - `_eyeRenderer` ← the rat's eye mesh renderer (or nearest child).
   - `_emissionProperty` — leave `_EmissionColor` for URP Lit.
   - Ensure the eye material has **Emission** enabled and is marked **Keyword: _EMISSION**.
4. **WeaponController**
   - Drag the active `ScopeOverlayController` into `_scopeOverlay`.
5. **OpticsTierConfig.asset**
   - For each tier, set:
     - `tierName`, `description`
     - `zoomLevel`, `FieldOfView` (legacy)
     - `supportedScopeModes` list (e.g.
       Tier 1 → `[Daylight]`,
       Tier 2 → `[Daylight, NightVision]`,
       Tier 3 → `[NightVision, ThermalGreen]`,
       Tier 4 → `[Daylight, NightVision, ThermalWhiteHot, ThermalGreen]`)
     - `defaultScopeMode`
     - `swayMultiplier` (recommend 1.0 → 0.6 as tier increases)
     - `aimAssistConeMultiplier` (1.1 → 0.85)

### Notes on `LightingModeController` vs `ScopeOverlayController`

Both touch URP post-processing but at different layers:
- `LightingModeController` (Level) sets **ambient / scene lighting** globally.
- `ScopeOverlayController` drives a **scope-only Volume** (attach to a GameObject parented to the scope camera, with a higher priority) so NV/Thermal looks apply through the scope but the world outside the scope stays readable if de-scoped.

Recommendation: use a **separate Volume** for the scope with Priority > any scene default Volumes, and do not reuse the LightingModeController volume.

---

## PHASE 3 – QA

### Script-level sanity

- All new scripts use the existing namespaces (`BarnSwarmSniper.Weapon`, `.Data`, `.UI`, `.AI`).
- All extensions to `WeaponPartCategory` were **appended**, preserving serialized enum indices of existing `.asset` files.
- `PlayerProgress` additions are JsonUtility-safe defaults (`Array.Empty<string>()`, `""`) and backward-compatible with old `playerProgress.json` saves (missing fields deserialise to defaults).
- `GameManager` never mutates `SettingsData` / `WeaponPartCatalog` / `OpticsTierConfig` — it reads them and builds a fresh `RuntimeLoadout` + `RuntimeSettings.FromSettingsData(...)` each mission.
- `UpgradeShopController` gracefully handles: missing catalog, empty category, missing managers, missing progress, missing icons, missing buttons. Errors surface to `_statusText` in red.
- `ScopeOverlayController.SwitchScopeMode` with a missing `ScopeVolumeProfileSet` falls back to sensible per-mode tints so the UI still visibly differs between modes when art is not yet wired.
- `WeaponController.ApplyModifiers` captures base aim-assist values once via `CaptureAimAssistBase`, so repeated calls (e.g. on tier change) don't compound the multiplier.

### Suggested in-editor smoke checks (run locally in Unity Editor)

1. **Build/compile**: Open project in Unity 2023 LTS → `Edit > Preferences` sanity → press `Ctrl+R` (refresh) → console should be error-free.
2. **MainMenu scene**: Enter Play mode → Shop button opens the shop → category tabs appear for each populated category → select an item → BUY (if enough pellets) → balance updates instantly → EQUIP → status shows "Equipped: <name>".
3. **Game scene**: Press Play from MainMenu → verify `GameManager.CurrentRuntimeLoadout` is non-null and aggregate reflects equipped parts → fire weapon, inspect `WeaponController` aim-assist fields at runtime.
4. **Scope modes**: While in-game, call `scopeOverlayController.SwitchScopeMode(ScopeMode.NightVision)` (hotkey or debug button) → screen tints green, reticle color changes, HUD accents tint green, rat eye-shine becomes bright green.
5. **Android build**: `File > Build Settings → Android → Switch Platform → Build`. Check that VolumeProfile assets are included in the build (they are referenced via `ScopeVolumeProfileSet` so Unity will pull them into the APK automatically).

### TODOs left for later phases (tracked but not implemented now)

- Shop art: real icons, NEW badge sprite, rarity borders, ShopItemRow/CategoryTabButton prefab visuals.
- ScopeOverlayController hotkey/gesture for switching modes in-game (logic is ready via `CycleScopeMode`).
- Hook `WeaponController.CycleScopeMode()` to a UI button / gesture.
- `magazineSizeDelta` and `noiseRadiusMultiplier` from `WeaponStatModifiers` are surfaced via `RuntimeLoadout.Aggregate` but not yet consumed by gameplay systems (no magazine/reload or noise-alert system exists yet — noted for Phase 3+).
- Cosmetic parts currently stack with functional stats at 1x — consider excluding them from aggregation in `RuntimeLoadout.Build` when a dedicated Cosmetic pipeline lands.
- Headshot telemetry + contract challenge completion: still stubbed in `ContractManager.IsChallengeComplete` (pre-existing TODO, untouched).
- Fix `ContractDefinition` `contractId` to always be unique + validated; `GameManager` now depends on it for prerequisite gating.

---

## ASSUMPTIONS

- Repository ships scripts flat at the repo root; the real Unity project places them under `Assets/Scripts/<area>/`. Namespaces are authoritative. Moving files to the proper subfolders does not require code changes.
- UGUI + TextMeshPro (confirmed by user); no UI Toolkit.
- URP is already enabled via `URP_SETUP_NOTES.md`.
- Final art (icons, reticles, NEW badge, rarity borders) will be supplied by the user later; the code wires Sprite/Color slots and degrades gracefully to placeholders.
