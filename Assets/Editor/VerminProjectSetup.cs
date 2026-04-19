#if UNITY_EDITOR
// Assets/Editor/VerminProjectSetup.cs
// One-click project setup for Vermin Exterminator.
//
// Usage (inside Unity Editor):
//   Vermin -> Setup All                         -> runs everything
//   Vermin -> 1. Create Data Assets             -> ScriptableObjects only
//   Vermin -> 2. Create MainMenu Scene          -> scene + canvas + wiring
//   Vermin -> 3. Configure Player Settings      -> Android + URP + orientation
//   Vermin -> 4. Add Scenes To Build Settings
//
// Usage from CLI:
//   Unity -projectPath . -executeMethod Vermin.Editor.VerminProjectSetup.RunAllFromCli

using System.Collections.Generic;
using System.IO;
using BarnSwarmSniper.Data;
using BarnSwarmSniper.Game;
using BarnSwarmSniper.Scoring;
using BarnSwarmSniper.UI;
using BarnSwarmSniper.Weapon;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Vermin.Editor
{
    public static class VerminProjectSetup
    {
        // --------------------------------------------------------------------
        // Paths
        // --------------------------------------------------------------------
        private const string DATA_FOLDER       = "Assets/Data";
        private const string CONTRACTS_FOLDER  = "Assets/Data/Contracts";
        private const string SCENES_FOLDER     = "Assets/Scenes";
        private const string SETTINGS_FOLDER   = "Assets/Settings";
        private const string MAIN_MENU_SCENE   = "Assets/Scenes/MainMenu.unity";
        private const string GAME_SCENE        = "Assets/Scenes/Game.unity";

        private const string SETTINGS_DATA_PATH      = DATA_FOLDER + "/SettingsData.asset";
        private const string OPTICS_TIER_PATH        = DATA_FOLDER + "/OpticsTierConfig.asset";
        private const string WEAPON_CATALOG_PATH     = DATA_FOLDER + "/WeaponPartCatalog.asset";
        private const string SCOPE_VOLUME_SET_PATH   = DATA_FOLDER + "/ScopeVolumeProfileSet.asset";
        private const string STORY_SET_PATH          = CONTRACTS_FOLDER + "/StoryContracts.asset";
        private const string DAILY_SET_PATH          = CONTRACTS_FOLDER + "/DailyContracts.asset";

        // --------------------------------------------------------------------
        // Menu entry points
        // --------------------------------------------------------------------
        [MenuItem("Vermin/Setup All", priority = 0)]
        public static void RunAll()
        {
            EditorUtility.DisplayProgressBar("Vermin Setup", "Configuring project settings...", 0.05f);
            try
            {
                ConfigurePlayerSettings();
                EditorUtility.DisplayProgressBar("Vermin Setup", "Creating data assets...", 0.25f);
                CreateDataAssets();
                EditorUtility.DisplayProgressBar("Vermin Setup", "Creating MainMenu scene...", 0.55f);
                CreateMainMenuScene();
                EditorUtility.DisplayProgressBar("Vermin Setup", "Adding scenes to Build Settings...", 0.9f);
                AddScenesToBuildSettings();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Vermin] Setup complete. Open Scenes/MainMenu.unity and press Play.");
            EditorUtility.DisplayDialog("Vermin Setup", "Setup complete.\n\nOpen Assets/Scenes/MainMenu.unity and press Play.", "OK");
        }

        /// <summary>CLI hook — no dialogs, exits cleanly.</summary>
        public static void RunAllFromCli()
        {
            ConfigurePlayerSettings();
            CreateDataAssets();
            CreateMainMenuScene();
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Vermin] CLI setup complete.");
        }

        [MenuItem("Vermin/1. Create Data Assets", priority = 20)]
        public static void CreateDataAssetsMenu() => CreateDataAssets();

        [MenuItem("Vermin/2. Create MainMenu Scene", priority = 21)]
        public static void CreateMainMenuSceneMenu() => CreateMainMenuScene();

        [MenuItem("Vermin/3. Configure Player Settings", priority = 22)]
        public static void ConfigurePlayerSettingsMenu() => ConfigurePlayerSettings();

        [MenuItem("Vermin/4. Add Scenes To Build Settings", priority = 23)]
        public static void AddScenesToBuildSettingsMenu() => AddScenesToBuildSettings();

        /// <summary>CLI hook for headless Android build.</summary>
        public static void BuildAndroidFromCli()
        {
            AddScenesToBuildSettings();
            var outDir = Path.Combine(Application.dataPath, "../Builds/Android");
            Directory.CreateDirectory(outDir);
            var outPath = Path.Combine(outDir, "VerminExterminator.apk");

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { MAIN_MENU_SCENE, GAME_SCENE },
                locationPathName = outPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[Vermin] Android build finished: {report.summary.result} -> {outPath}");
        }

        // --------------------------------------------------------------------
        // 1) Player / Graphics / Android settings
        // --------------------------------------------------------------------
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "Vermin Exterminator";
            if (string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany")
                PlayerSettings.companyName = "BarnSwarm";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft  = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait       = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Android
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            var pkg = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
            if (string.IsNullOrEmpty(pkg) || pkg.StartsWith("com.DefaultCompany"))
            {
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.barnswarm.vermininfestation");
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[Vermin] Switching active build target to Android (this may take a few minutes)...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(UnityEditor.Build.NamedBuildTarget.Android, BuildTarget.Android);
            }

            Debug.Log("[Vermin] Player / Android settings configured.");
        }

        // --------------------------------------------------------------------
        // 2) Create all ScriptableObject data assets
        // --------------------------------------------------------------------
        public static void CreateDataAssets()
        {
            EnsureFolder(DATA_FOLDER);
            EnsureFolder(CONTRACTS_FOLDER);

            // SettingsData
            GetOrCreateAsset<SettingsData>(SETTINGS_DATA_PATH);

            // OpticsTierConfig
            var optics = GetOrCreateAsset<OpticsTierConfig>(OPTICS_TIER_PATH);
            if (optics.OpticsTiers == null || optics.OpticsTiers.Length == 0)
            {
                optics.OpticsTiers = new[]
                {
                    MakeTier("Iron Sights",    1f,  60f, ScopeOverlayController.ScopeMode.Daylight, 1.2f, 1.1f,
                        new[] { ScopeOverlayController.ScopeMode.Daylight },
                        "Starter iron sights. No magnification."),
                    MakeTier("2x Hunter",      2f,  30f, ScopeOverlayController.ScopeMode.Daylight, 1.0f, 1.0f,
                        new[] { ScopeOverlayController.ScopeMode.Daylight, ScopeOverlayController.ScopeMode.NightVision },
                        "Light magnification with basic NV."),
                    MakeTier("4x Tactical",    4f,  15f, ScopeOverlayController.ScopeMode.NightVision, 0.8f, 0.9f,
                        new[] { ScopeOverlayController.ScopeMode.NightVision, ScopeOverlayController.ScopeMode.ThermalGreen },
                        "Medium-range tactical optic with thermal."),
                    MakeTier("8x Hybrid",      8f,  7.5f, ScopeOverlayController.ScopeMode.ThermalWhiteHot, 0.6f, 0.85f,
                        new[] { ScopeOverlayController.ScopeMode.Daylight, ScopeOverlayController.ScopeMode.NightVision,
                                ScopeOverlayController.ScopeMode.ThermalWhiteHot, ScopeOverlayController.ScopeMode.ThermalGreen },
                        "Long-range hybrid optic. All modes.")
                };
                EditorUtility.SetDirty(optics);
            }

            // WeaponPartCatalog
            var catalog = GetOrCreateAsset<WeaponPartCatalog>(WEAPON_CATALOG_PATH);
            if (catalog.parts == null || catalog.parts.Count == 0)
            {
                catalog.parts = new List<WeaponPartDefinition>
                {
                    MakePart("scope_iron",   "Iron Sights",       WeaponPartCategory.Scope,   0,   1, WeaponPartRarity.Common,
                        "Factory-issued iron sights. No magnification.", opticsTierIndex: 0),
                    MakePart("scope_tier2",  "2x Hunter Scope",   WeaponPartCategory.Scope,   50,  1, WeaponPartRarity.Uncommon,
                        "Light-magnification optic with basic night vision.", opticsTierIndex: 1, sway: 0.95f),
                    MakePart("scope_tier3",  "4x Tactical Scope", WeaponPartCategory.Scope,   200, 3, WeaponPartRarity.Rare,
                        "Medium-range tactical optic with thermal imaging.", opticsTierIndex: 2, sway: 0.85f),
                    MakePart("barrel_quiet", "Suppressed Barrel", WeaponPartCategory.Barrel,  75,  2, WeaponPartRarity.Uncommon,
                        "Reduces audible report and muzzle flash.", recoil: 0.9f, noise: 0.5f),
                    MakePart("trigger_tuned","Tuned Trigger",     WeaponPartCategory.Trigger, 100, 2, WeaponPartRarity.Rare,
                        "Competition-grade trigger group. Faster follow-up shots.", fireRate: 1.2f),
                    MakePart("ammo_match",   "Match-Grade Pellets", WeaponPartCategory.Ammo,  40,  1, WeaponPartRarity.Uncommon,
                        "Precision-manufactured rounds. Reduced recoil.", recoil: 0.85f),
                    MakePart("stock_heavy",  "Heavy Match Stock", WeaponPartCategory.Stock,   60,  1, WeaponPartRarity.Common,
                        "Weighted stock. Reduces sway at cost of fire rate.", sway: 0.85f, fireRate: 0.95f),
                };
                EditorUtility.SetDirty(catalog);
            }

            // ScopeVolumeProfileSet
            var volSet = GetOrCreateAsset<ScopeVolumeProfileSet>(SCOPE_VOLUME_SET_PATH);
            if (volSet.entries == null || volSet.entries.Count == 0)
            {
                volSet.entries = new List<ScopeVolumeProfileSet.Entry>
                {
                    new ScopeVolumeProfileSet.Entry { mode = ScopeOverlayController.ScopeMode.Daylight,
                        reticleColor = Color.white, hudAccentColor = Color.white,
                        ratEyeShineColor = new Color(0.8f, 0.1f, 0.1f), ratEyeShineIntensity = 0.2f },
                    new ScopeVolumeProfileSet.Entry { mode = ScopeOverlayController.ScopeMode.NightVision,
                        reticleColor = new Color(0.3f, 1f, 0.3f), hudAccentColor = new Color(0.3f, 1f, 0.3f),
                        ratEyeShineColor = new Color(0.2f, 1f, 0.2f), ratEyeShineIntensity = 3f },
                    new ScopeVolumeProfileSet.Entry { mode = ScopeOverlayController.ScopeMode.ThermalWhiteHot,
                        reticleColor = Color.white, hudAccentColor = Color.white,
                        ratEyeShineColor = Color.white, ratEyeShineIntensity = 4f },
                    new ScopeVolumeProfileSet.Entry { mode = ScopeOverlayController.ScopeMode.ThermalGreen,
                        reticleColor = new Color(0.4f, 1f, 0.5f), hudAccentColor = new Color(0.4f, 1f, 0.5f),
                        ratEyeShineColor = new Color(0.4f, 1f, 0.5f), ratEyeShineIntensity = 3.5f },
                };
                EditorUtility.SetDirty(volSet);
            }

            // Contracts
            var story01 = GetOrCreateContract(CONTRACTS_FOLDER + "/Contract_Story_01.asset",
                "story_01", "First Barn", "Clear the starter barn of vermin.", 1, 10);
            var story02 = GetOrCreateContract(CONTRACTS_FOLDER + "/Contract_Story_02.asset",
                "story_02", "Abandoned Silo", "Sweep the silo complex.", 2, 20);
            var daily01 = GetOrCreateContract(CONTRACTS_FOLDER + "/Contract_Daily_01.asset",
                "daily_01", "Daily Sweep", "Quick contract rotation.", 1, 15);

            var storySet = GetOrCreateAsset<ContractSet>(STORY_SET_PATH);
            storySet.setTag = "Story";
            storySet.contracts = new List<ContractDefinition> { story01, story02 };
            EditorUtility.SetDirty(storySet);

            var dailySet = GetOrCreateAsset<ContractSet>(DAILY_SET_PATH);
            dailySet.setTag = "Daily";
            dailySet.contracts = new List<ContractDefinition> { daily01 };
            EditorUtility.SetDirty(dailySet);

            AssetDatabase.SaveAssets();
            Debug.Log("[Vermin] Data assets created in Assets/Data/.");
        }

        // --------------------------------------------------------------------
        // 3) Create the MainMenu scene + GameSystems + Canvas + wiring
        // --------------------------------------------------------------------
        public static void CreateMainMenuScene()
        {
            EnsureFolder(SCENES_FOLDER);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // ---- GameSystems ----
            var gameSystems = new GameObject("GameSystems");
            var progressMgr = gameSystems.AddComponent<PlayerProgressManager>();
            var settingsMgr = gameSystems.AddComponent<SettingsManager>();
            var scoreMgr    = gameSystems.AddComponent<ScoreManager>();
            var currencyMgr = gameSystems.AddComponent<CurrencyManager>();
            var contractMgr = gameSystems.AddComponent<ContractManager>();
            var gameMgr     = gameSystems.AddComponent<GameManager>();

            var catalog  = AssetDatabase.LoadAssetAtPath<WeaponPartCatalog>(WEAPON_CATALOG_PATH);
            var storySet = AssetDatabase.LoadAssetAtPath<ContractSet>(STORY_SET_PATH);
            var dailySet = AssetDatabase.LoadAssetAtPath<ContractSet>(DAILY_SET_PATH);

            SetPrivateField(gameMgr, "_playerProgressManager", progressMgr);
            SetPrivateField(gameMgr, "_settingsManager", settingsMgr);
            SetPrivateField(gameMgr, "_scoreManager", scoreMgr);
            SetPrivateField(gameMgr, "_currencyManager", currencyMgr);
            SetPrivateField(gameMgr, "_contractManager", contractMgr);
            SetPrivateField(gameMgr, "_weaponPartCatalog", catalog);
            SetPrivateField(gameMgr, "_mainMenuSceneName", "MainMenu");
            SetPrivateField(gameMgr, "_gameSceneName", "Game");
            SetPrivateField(gameMgr, "_levelDurationSeconds", 120f);

            SetPrivateField(contractMgr, "_storyContracts", storySet);
            SetPrivateField(contractMgr, "_dailyContracts", dailySet);
            SetPrivateField(contractMgr, "_playerProgressManager", progressMgr);
            SetPrivateField(contractMgr, "_scoreManager", scoreMgr);
            SetPrivateField(contractMgr, "_currencyManager", currencyMgr);

            // ---- Canvas ----
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            // ---- Panels ----
            var mainPanel     = CreatePanel(canvasGo.transform, "MainMenuPanel", new Color(0.08f, 0.08f, 0.12f, 1f));
            var settingsPanel = CreatePanel(canvasGo.transform, "SettingsPanel", new Color(0.05f, 0.05f, 0.08f, 1f));
            var shopPanel     = CreatePanel(canvasGo.transform, "ShopPanel",     new Color(0.06f, 0.06f, 0.10f, 1f));
            settingsPanel.SetActive(false);
            shopPanel.SetActive(false);

            // Main menu layout
            var vlayout = mainPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.padding = new RectOffset(80, 80, 80, 80);
            vlayout.spacing = 16;
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.childControlWidth = true;
            vlayout.childControlHeight = false;
            vlayout.childForceExpandWidth = false;
            vlayout.childForceExpandHeight = false;

            var labelTitle    = CreateTMPLabel(mainPanel.transform, "Label_Title",    "VERMIN EXTERMINATOR", 56, FontStyles.Bold);
            var labelLevel    = CreateTMPLabel(mainPanel.transform, "Label_Level",    "LEVEL: 1", 28);
            var labelPellets  = CreateTMPLabel(mainPanel.transform, "Label_Pellets",  "PELLETS: 0", 28);
            var labelOptics   = CreateTMPLabel(mainPanel.transform, "Label_Optics",   "OPTICS TIER: 1", 24);
            var labelStory    = CreateTMPLabel(mainPanel.transform, "Label_Story",    "STORY: —", 22);
            var labelDaily    = CreateTMPLabel(mainPanel.transform, "Label_Daily",    "DAILY: —", 22);

            var btnPlay       = CreateTMPButton(mainPanel.transform, "Button_Play",      "PLAY");
            var btnStory      = CreateTMPButton(mainPanel.transform, "Button_PlayStory", "PLAY STORY");
            var btnDaily      = CreateTMPButton(mainPanel.transform, "Button_PlayDaily", "PLAY DAILY");
            var btnShop       = CreateTMPButton(mainPanel.transform, "Button_Shop",      "SHOP");
            var btnSettings   = CreateTMPButton(mainPanel.transform, "Button_Settings",  "SETTINGS");
            var btnExit       = CreateTMPButton(mainPanel.transform, "Button_Exit",      "EXIT");

            // Settings panel: just a close button for now
            var btnCloseSettings = CreateTMPButton(settingsPanel.transform, "Button_CloseSettings", "CLOSE");
            CenterInRect(btnCloseSettings.gameObject);

            // Shop panel layout
            BuildShopPanel(shopPanel, out var closeShopBtn, out var upgradeShop);

            // ---- MainMenuController ----
            var menuControllerGo = new GameObject("MenuController");
            menuControllerGo.transform.SetParent(canvasGo.transform, false);
            var menuController = menuControllerGo.AddComponent<MainMenuController>();
            SetPrivateField(menuController, "_playButton",              btnPlay);
            SetPrivateField(menuController, "_playStoryContractButton", btnStory);
            SetPrivateField(menuController, "_playDailyContractButton", btnDaily);
            SetPrivateField(menuController, "_settingsButton",          btnSettings);
            SetPrivateField(menuController, "_closeSettingsButton",     btnCloseSettings);
            SetPrivateField(menuController, "_shopButton",              btnShop);
            SetPrivateField(menuController, "_closeShopButton",         closeShopBtn);
            SetPrivateField(menuController, "_exitButton",              btnExit);
            SetPrivateField(menuController, "_currentLevelText",        labelLevel);
            SetPrivateField(menuController, "_pelletsText",             labelPellets);
            SetPrivateField(menuController, "_opticsTierText",          labelOptics);
            SetPrivateField(menuController, "_storyContractText",       labelStory);
            SetPrivateField(menuController, "_dailyContractText",       labelDaily);
            SetPrivateField(menuController, "_mainMenuPanel",           mainPanel);
            SetPrivateField(menuController, "_settingsPanel",           settingsPanel);
            SetPrivateField(menuController, "_shopPanel",               shopPanel);

            // ---- UpgradeShopController wiring (catalog) ----
            SetPrivateField(upgradeShop, "_catalog", catalog);

            // ---- Save ----
            EditorSceneManager.SaveScene(scene, MAIN_MENU_SCENE);
            Debug.Log("[Vermin] MainMenu scene created at " + MAIN_MENU_SCENE);
        }

        // --------------------------------------------------------------------
        // 4) Scenes to Build Settings
        // --------------------------------------------------------------------
        public static void AddScenesToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            if (File.Exists(MAIN_MENU_SCENE))
                scenes.Add(new EditorBuildSettingsScene(MAIN_MENU_SCENE, true));
            if (File.Exists(GAME_SCENE))
                scenes.Add(new EditorBuildSettingsScene(GAME_SCENE, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Vermin] Build Settings scenes updated.");
        }

        // --------------------------------------------------------------------
        // Shop panel builder
        // --------------------------------------------------------------------
        private static void BuildShopPanel(GameObject shopPanel, out Button closeShopBtn, out UpgradeShopController shopCtrl)
        {
            shopCtrl = shopPanel.AddComponent<UpgradeShopController>();

            // Header
            var header = CreateSubPanel(shopPanel.transform, "Header",
                anchorMin: new Vector2(0, 0.92f), anchorMax: new Vector2(1, 1f));
            var pelletsText = CreateTMPLabel(header.transform, "Label_Pellets", "PELLETS: 0", 26, FontStyles.Bold);
            PlaceRect(pelletsText.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.3f, 1f));
            var statusText = CreateTMPLabel(header.transform, "Label_Status", "", 22);
            PlaceRect(statusText.rectTransform, new Vector2(0.32f, 0f), new Vector2(0.85f, 1f));
            closeShopBtn = CreateTMPButton(header.transform, "Button_CloseShop", "CLOSE");
            PlaceRect(closeShopBtn.GetComponent<RectTransform>(), new Vector2(0.87f, 0.15f), new Vector2(0.98f, 0.85f));

            // Category tabs row
            var tabsRow = CreateSubPanel(shopPanel.transform, "CategoryTabs",
                anchorMin: new Vector2(0, 0.84f), anchorMax: new Vector2(1, 0.92f));
            var hLayout = tabsRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(16, 16, 8, 8);
            hLayout.spacing = 8;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = false;
            hLayout.childForceExpandWidth = false;

            // Item list (ScrollView)
            var scrollGo = new GameObject("ItemScrollView",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(shopPanel.transform, false);
            var scrollRT = scrollGo.GetComponent<RectTransform>();
            PlaceRect(scrollRT, new Vector2(0, 0.08f), new Vector2(0.45f, 0.83f));
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            PlaceRect(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 100);
            var contentVL = content.GetComponent<VerticalLayoutGroup>();
            contentVL.padding = new RectOffset(8, 8, 8, 8);
            contentVL.spacing = 6;
            contentVL.childControlHeight = false;
            contentVL.childForceExpandHeight = false;
            contentVL.childControlWidth = true;
            contentVL.childForceExpandWidth = true;
            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRT;
            scroll.horizontal = false;
            scroll.vertical = true;

            // Detail panel
            var detail = CreateSubPanel(shopPanel.transform, "DetailPanel",
                anchorMin: new Vector2(0.47f, 0.08f), anchorMax: new Vector2(1f, 0.83f));
            var detailVL = detail.AddComponent<VerticalLayoutGroup>();
            detailVL.padding = new RectOffset(20, 20, 20, 20);
            detailVL.spacing = 8;
            detailVL.childForceExpandHeight = false;
            detailVL.childControlHeight = true;
            detailVL.childControlWidth = true;
            detailVL.childForceExpandWidth = true;

            var detailName = CreateTMPLabel(detail.transform, "DetailName",        "— Select a part —", 28, FontStyles.Bold);
            var detailRarity = CreateTMPLabel(detail.transform, "DetailRarity",    "",                24);
            var detailDesc = CreateTMPLabel(detail.transform, "DetailDescription", "",                20);
            var detailReqs = CreateTMPLabel(detail.transform, "DetailRequirements","",                18);
            var statsCur = CreateTMPLabel(detail.transform, "StatsCurrent",        "Current loadout", 18);
            var statsWith = CreateTMPLabel(detail.transform, "StatsWith",          "With this part",  18);

            var buyBtn = CreateTMPButton(detail.transform, "Button_Buy", "BUY");
            var equipBtn = CreateTMPButton(detail.transform, "Button_Equip", "EQUIP");

            // Item prefab (created at runtime — stored in Assets/Prefabs/UI/ShopItemRow.prefab)
            var itemPrefab = CreateShopItemPrefab();
            var categoryPrefab = CreateCategoryTabPrefab();

            // Empty-state label
            var empty = CreateTMPLabel(shopPanel.transform, "EmptyState",
                "No parts in this category yet.\nAdd entries to WeaponPartCatalog.", 22);
            empty.alignment = TextAlignmentOptions.Center;
            PlaceRect(empty.rectTransform, new Vector2(0.1f, 0.3f), new Vector2(0.45f, 0.6f));
            empty.gameObject.SetActive(false);

            // Wire UpgradeShopController
            SetPrivateField(shopCtrl, "_pelletsText", pelletsText);
            SetPrivateField(shopCtrl, "_statusText", statusText);
            SetPrivateField(shopCtrl, "_closeButton", closeShopBtn);
            SetPrivateField(shopCtrl, "_categoryTabsContainer", tabsRow.transform);
            SetPrivateField(shopCtrl, "_categoryTabPrefab", categoryPrefab);
            SetPrivateField(shopCtrl, "_itemListContainer", content.transform);
            SetPrivateField(shopCtrl, "_itemButtonPrefab", itemPrefab);
            SetPrivateField(shopCtrl, "_detailName", detailName);
            SetPrivateField(shopCtrl, "_detailRarity", detailRarity);
            SetPrivateField(shopCtrl, "_detailDescription", detailDesc);
            SetPrivateField(shopCtrl, "_detailRequirements", detailReqs);
            SetPrivateField(shopCtrl, "_detailStatsCurrent", statsCur);
            SetPrivateField(shopCtrl, "_detailStatsWith", statsWith);
            SetPrivateField(shopCtrl, "_buyButton", buyBtn);
            SetPrivateField(shopCtrl, "_equipButton", equipBtn);
            SetPrivateField(shopCtrl, "_buyButtonLabel", buyBtn.GetComponentInChildren<TextMeshProUGUI>());
            SetPrivateField(shopCtrl, "_equipButtonLabel", equipBtn.GetComponentInChildren<TextMeshProUGUI>());
            SetPrivateField(shopCtrl, "_emptyStateLabel", empty.gameObject);
        }

        private static Button CreateCategoryTabPrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");
            var path = "Assets/Prefabs/UI/CategoryTabButton.prefab";
            if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<Button>(path);

            var go = new GameObject("CategoryTabButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 50);
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

            var label = CreateTMPLabel(go.transform, "Label", "CATEGORY", 18, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            PlaceRect(label.rectTransform, Vector2.zero, Vector2.one);

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved.GetComponent<Button>();
        }

        private static ShopItemButton CreateShopItemPrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");
            var path = "Assets/Prefabs/UI/ShopItemRow.prefab";
            if (File.Exists(path))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                return prefab.GetComponent<ShopItemButton>();
            }

            var go = new GameObject("ShopItemRow", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 64;

            // Children
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            PlaceRect(iconGo.GetComponent<RectTransform>(), new Vector2(0.01f, 0.1f), new Vector2(0.12f, 0.9f));

            var nameText = CreateTMPLabel(go.transform, "Label_Name", "Name", 18, FontStyles.Bold);
            PlaceRect(nameText.rectTransform, new Vector2(0.14f, 0.55f), new Vector2(0.7f, 0.95f));

            var statsText = CreateTMPLabel(go.transform, "Label_Stats", "", 14);
            PlaceRect(statsText.rectTransform, new Vector2(0.14f, 0.05f), new Vector2(0.7f, 0.5f));

            var costText = CreateTMPLabel(go.transform, "Label_Cost", "0 P", 18, FontStyles.Bold);
            PlaceRect(costText.rectTransform, new Vector2(0.72f, 0.1f), new Vector2(0.98f, 0.9f));
            costText.alignment = TextAlignmentOptions.MidlineRight;

            var rarityText = CreateTMPLabel(go.transform, "Label_Rarity", "", 12);
            PlaceRect(rarityText.rectTransform, new Vector2(0.72f, 0.55f), new Vector2(0.98f, 0.78f));
            rarityText.alignment = TextAlignmentOptions.MidlineRight;

            // Badges as empty placeholders
            var lockGo = CreateBadge(go.transform, "Lock_Icon", new Color(0.9f, 0.3f, 0.3f, 0.7f));
            var ownedGo = CreateBadge(go.transform, "Owned_Badge", new Color(0.4f, 0.9f, 0.4f, 0.7f));
            var equippedGo = CreateBadge(go.transform, "Equipped_Badge", new Color(1f, 0.85f, 0.25f, 0.9f));
            var newGo = CreateBadge(go.transform, "New_Badge", new Color(0.3f, 0.7f, 1f, 0.9f));

            var sib = go.AddComponent<ShopItemButton>();
            SetPrivateField(sib, "_button", go.GetComponent<Button>());
            SetPrivateField(sib, "_iconImage", iconGo.GetComponent<Image>());
            SetPrivateField(sib, "_nameText", nameText);
            SetPrivateField(sib, "_costText", costText);
            SetPrivateField(sib, "_rarityText", rarityText);
            SetPrivateField(sib, "_shortStatsText", statsText);
            SetPrivateField(sib, "_lockIcon", lockGo);
            SetPrivateField(sib, "_ownedBadge", ownedGo);
            SetPrivateField(sib, "_equippedBadge", equippedGo);
            SetPrivateField(sib, "_newBadge", newGo);

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved.GetComponent<ShopItemButton>();
        }

        private static GameObject CreateBadge(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            PlaceRect(go.GetComponent<RectTransform>(), new Vector2(0.8f, 0.78f), new Vector2(0.88f, 0.95f));
            go.SetActive(false);
            return go;
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------
        private static OpticsTierConfig.OpticsTier MakeTier(string name, float zoom, float fov,
            ScopeOverlayController.ScopeMode defaultMode, float sway, float aimCone,
            ScopeOverlayController.ScopeMode[] supported, string desc)
        {
            var tier = new OpticsTierConfig.OpticsTier
            {
                tierName = name,
                description = desc,
                zoomLevel = zoom,
                FieldOfView = fov,
                defaultScopeMode = defaultMode,
                swayMultiplier = sway,
                aimAssistConeMultiplier = aimCone,
                supportedScopeModes = new List<ScopeOverlayController.ScopeMode>(supported)
            };
            return tier;
        }

        private static WeaponPartDefinition MakePart(string id, string name, WeaponPartCategory cat,
            int cost, int level, WeaponPartRarity rarity, string desc,
            int opticsTierIndex = -1, float fireRate = 1f, float recoil = 1f, float sway = 1f, float noise = 1f)
        {
            return new WeaponPartDefinition
            {
                id = id,
                displayName = name,
                description = desc,
                category = cat,
                rarity = rarity,
                costPellets = cost,
                requiredPlayerLevel = level,
                opticsTierIndex = opticsTierIndex,
                modifiers = new WeaponStatModifiers
                {
                    fireRateMultiplier = fireRate,
                    recoilAmountMultiplier = recoil,
                    swayMultiplier = sway,
                    noiseRadiusMultiplier = noise,
                },
            };
        }

        private static ContractDefinition GetOrCreateContract(string path, string id, string display, string desc, int reqLevel, int reward)
        {
            var c = AssetDatabase.LoadAssetAtPath<ContractDefinition>(path);
            if (c == null)
            {
                c = ScriptableObject.CreateInstance<ContractDefinition>();
                AssetDatabase.CreateAsset(c, path);
            }
            c.contractId = id;
            c.displayName = display;
            c.description = desc;
            c.requiredPlayerLevel = reqLevel;
            c.basePelletReward = reward;
            EditorUtility.SetDirty(c);
            return c;
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            PlaceRect(rt, Vector2.zero, Vector2.one);
            go.GetComponent<Image>().color = bg;
            return go;
        }

        private static GameObject CreateSubPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            PlaceRect(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.15f);
            return go;
        }

        private static TextMeshProUGUI CreateTMPLabel(Transform parent, string name, string text, int size, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = size + 10;
            return tmp;
        }

        private static Button CreateTMPButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);
            go.GetComponent<LayoutElement>().preferredHeight = 56;
            go.GetComponent<LayoutElement>().preferredWidth = 320;
            var text = CreateTMPLabel(go.transform, "Label", label, 24, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            PlaceRect(text.rectTransform, Vector2.zero, Vector2.one);
            return go.GetComponent<Button>();
        }

        private static void PlaceRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
        }

        private static void CenterInRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.4f, 0.4f);
            rt.anchorMax = new Vector2(0.6f, 0.55f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Reflection helper to set private [SerializeField] fields. Used because these wire only at runtime otherwise.</summary>
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var t = target.GetType();
            while (t != null)
            {
                var f = t.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (f != null)
                {
                    f.SetValue(target, value);
                    if (target is Object uo) EditorUtility.SetDirty(uo);
                    return;
                }
                t = t.BaseType;
            }
            Debug.LogWarning($"[Vermin] Field '{fieldName}' not found on {target.GetType().Name}. Wiring skipped.");
        }
    }
}
#endif
