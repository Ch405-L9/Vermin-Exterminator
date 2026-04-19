#if UNITY_EDITOR
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
        private const string DATA_FOLDER       = "Assets/Data";
        private const string CONTRACTS_FOLDER  = "Assets/Data/Contracts";
        private const string SCENES_FOLDER     = "Assets/Scenes";
        private const string MAIN_MENU_SCENE   = "Assets/Scenes/MainMenu.unity";
        private const string GAME_SCENE        = "Assets/Scenes/Game.unity";
        private const string SETTINGS_DATA_PATH    = DATA_FOLDER + "/SettingsData.asset";
        private const string OPTICS_TIER_PATH      = DATA_FOLDER + "/OpticsTierConfig.asset";
        private const string WEAPON_CATALOG_PATH   = DATA_FOLDER + "/WeaponPartCatalog.asset";
        private const string SCOPE_VOLUME_SET_PATH = DATA_FOLDER + "/ScopeVolumeProfileSet.asset";
        private const string STORY_SET_PATH        = CONTRACTS_FOLDER + "/StoryContracts.asset";
        private const string DAILY_SET_PATH        = CONTRACTS_FOLDER + "/DailyContracts.asset";

        [MenuItem("Vermin/Setup All", priority = 0)]
        public static void RunAll()
        {
            EditorUtility.DisplayProgressBar("Vermin Setup", "Configuring...", 0.05f);
            try
            {
                ConfigurePlayerSettings();
                EditorUtility.DisplayProgressBar("Vermin Setup", "Creating data...", 0.25f);
                CreateDataAssets();
                EditorUtility.DisplayProgressBar("Vermin Setup", "Creating scene...", 0.55f);
                CreateMainMenuScene();
                EditorUtility.DisplayProgressBar("Vermin Setup", "Build settings...", 0.9f);
                AddScenesToBuildSettings();
            }
            finally { EditorUtility.ClearProgressBar(); }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Vermin] Setup complete.");
            EditorUtility.DisplayDialog("Vermin Setup", "Setup complete.\n\nOpen Assets/Scenes/MainMenu.unity and press Play.", "OK");
        }

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

        public static void BuildAndroidFromCli()
        {
            AddScenesToBuildSettings();
            var outDir = Path.Combine(Application.dataPath, "../Builds/Android");
            Directory.CreateDirectory(outDir);
            var outPath = Path.Combine(outDir, "VerminExterminator.apk");
            var opts = new BuildPlayerOptions {
                scenes = new[] { MAIN_MENU_SCENE, GAME_SCENE },
                locationPathName = outPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[Vermin] Android build: {report.summary.result} -> {outPath}");
        }

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
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            var pkg = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
            if (string.IsNullOrEmpty(pkg) || pkg.StartsWith("com.DefaultCompany"))
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.barnswarm.vermininfestation");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[Vermin] Switching to Android build target...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(UnityEditor.Build.NamedBuildTarget.Android, BuildTarget.Android);
            }
            Debug.Log("[Vermin] Player settings configured.");
        }

        public static void CreateDataAssets()
        {
            EnsureFolder(DATA_FOLDER);
            EnsureFolder(CONTRACTS_FOLDER);
            GetOrCreateAsset<SettingsData>(SETTINGS_DATA_PATH);

            var optics = GetOrCreateAsset<OpticsTierConfig>(OPTICS_TIER_PATH);
            if (optics.OpticsTiers == null || optics.OpticsTiers.Length == 0)
            {
                optics.OpticsTiers = new[]
                {
                    MakeTier("Iron Sights", 1f, 60f, ScopeOverlayController.ScopeMode.Daylight, 1.2f, 1.1f,
                        new[]{ScopeOverlayController.ScopeMode.Daylight}, "Starter iron sights."),
                    MakeTier("2x Hunter", 2f, 30f, ScopeOverlayController.ScopeMode.Daylight, 1.0f, 1.0f,
                        new[]{ScopeOverlayController.ScopeMode.Daylight, ScopeOverlayController.ScopeMode.NightVision}, "Light magnification + basic NV."),
                    MakeTier("4x Tactical", 4f, 15f, ScopeOverlayController.ScopeMode.NightVision, 0.8f, 0.9f,
                        new[]{ScopeOverlayController.ScopeMode.NightVision, ScopeOverlayController.ScopeMode.ThermalGreen}, "Tactical optic with thermal."),
                    MakeTier("8x Hybrid", 8f, 7.5f, ScopeOverlayController.ScopeMode.ThermalWhiteHot, 0.6f, 0.85f,
                        new[]{ScopeOverlayController.ScopeMode.Daylight, ScopeOverlayController.ScopeMode.NightVision,
                              ScopeOverlayController.ScopeMode.ThermalWhiteHot, ScopeOverlayController.ScopeMode.ThermalGreen}, "All modes.")
                };
                EditorUtility.SetDirty(optics);
            }

            var catalog = GetOrCreateAsset<WeaponPartCatalog>(WEAPON_CATALOG_PATH);
            if (catalog.parts == null || catalog.parts.Count == 0)
            {
                catalog.parts = new List<WeaponPartDefinition> {
                    MakePart("scope_iron",    "Iron Sights",       WeaponPartCategory.Scope,   0,   1, WeaponPartRarity.Common,    "Factory iron sights.", opticsTierIndex: 0),
                    MakePart("scope_tier2",   "2x Hunter Scope",   WeaponPartCategory.Scope,   50,  1, WeaponPartRarity.Uncommon,  "Light mag + NV.", opticsTierIndex: 1, sway: 0.95f),
                    MakePart("scope_tier3",   "4x Tactical Scope", WeaponPartCategory.Scope,   200, 3, WeaponPartRarity.Rare,      "Thermal tactical optic.", opticsTierIndex: 2, sway: 0.85f),
                    MakePart("barrel_quiet",  "Suppressed Barrel", WeaponPartCategory.Barrel,  75,  2, WeaponPartRarity.Uncommon,  "Reduces noise.", recoil: 0.9f, noise: 0.5f),
                    MakePart("trigger_tuned", "Tuned Trigger",     WeaponPartCategory.Trigger, 100, 2, WeaponPartRarity.Rare,      "Faster follow-ups.", fireRate: 1.2f),
                    MakePart("ammo_match",    "Match-Grade Pellets",WeaponPartCategory.Ammo,   40,  1, WeaponPartRarity.Uncommon,  "Reduced recoil.", recoil: 0.85f),
                    MakePart("stock_heavy",   "Heavy Match Stock", WeaponPartCategory.Stock,   60,  1, WeaponPartRarity.Common,    "Less sway.", sway: 0.85f, fireRate: 0.95f),
                };
                EditorUtility.SetDirty(catalog);
            }

            var volSet = GetOrCreateAsset<ScopeVolumeProfileSet>(SCOPE_VOLUME_SET_PATH);
            if (volSet.entries == null || volSet.entries.Count == 0)
            {
                volSet.entries = new List<ScopeVolumeProfileSet.Entry> {
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

            var s1 = GetOrCreateContract(CONTRACTS_FOLDER + "/Contract_Story_01.asset", "story_01", "First Barn", "Clear the starter barn.", 1, 10);
            var s2 = GetOrCreateContract(CONTRACTS_FOLDER + "/Contract_Story_02.asset", "story_02", "Abandoned Silo", "Sweep the silo.", 2, 20);
            var d1 = GetOrCreateContract(CONTRACTS_FOLDER + "/Contract_Daily_01.asset", "daily_01", "Daily Sweep", "Daily rotation.", 1, 15);

            var storySet = GetOrCreateAsset<ContractSet>(STORY_SET_PATH);
            storySet.setTag = "Story";
            storySet.contracts = new List<ContractDefinition> { s1, s2 };
            EditorUtility.SetDirty(storySet);

            var dailySet = GetOrCreateAsset<ContractSet>(DAILY_SET_PATH);
            dailySet.setTag = "Daily";
            dailySet.contracts = new List<ContractDefinition> { d1 };
            EditorUtility.SetDirty(dailySet);

            AssetDatabase.SaveAssets();
            Debug.Log("[Vermin] Data assets created.");
        }

        public static void CreateMainMenuScene()
        {
            EnsureFolder(SCENES_FOLDER);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var gs = new GameObject("GameSystems");
            var progress = gs.AddComponent<PlayerProgressManager>();
            var settings = gs.AddComponent<SettingsManager>();
            var score    = gs.AddComponent<ScoreManager>();
            var currency = gs.AddComponent<CurrencyManager>();
            var contract = gs.AddComponent<ContractManager>();
            var game     = gs.AddComponent<GameManager>();

            var catalog  = AssetDatabase.LoadAssetAtPath<WeaponPartCatalog>(WEAPON_CATALOG_PATH);
            var storySet = AssetDatabase.LoadAssetAtPath<ContractSet>(STORY_SET_PATH);
            var dailySet = AssetDatabase.LoadAssetAtPath<ContractSet>(DAILY_SET_PATH);

            SetField(game, "_playerProgressManager", progress);
            SetField(game, "_settingsManager", settings);
            SetField(game, "_scoreManager", score);
            SetField(game, "_currencyManager", currency);
            SetField(game, "_contractManager", contract);
            SetField(game, "_weaponPartCatalog", catalog);
            SetField(game, "_mainMenuSceneName", "MainMenu");
            SetField(game, "_gameSceneName", "Game");
            SetField(game, "_levelDurationSeconds", 120f);

            SetField(contract, "_storyContracts", storySet);
            SetField(contract, "_dailyContracts", dailySet);
            SetField(contract, "_playerProgressManager", progress);
            SetField(contract, "_scoreManager", score);
            SetField(contract, "_currencyManager", currency);

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = canvasGo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var mainPanel     = MakePanel(canvasGo.transform, "MainMenuPanel", new Color(0.08f, 0.08f, 0.12f));
            var settingsPanel = MakePanel(canvasGo.transform, "SettingsPanel", new Color(0.05f, 0.05f, 0.08f));
            var shopPanel     = MakePanel(canvasGo.transform, "ShopPanel",     new Color(0.06f, 0.06f, 0.10f));
            settingsPanel.SetActive(false);
            shopPanel.SetActive(false);

            var vl = mainPanel.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(80,80,80,80); vl.spacing = 16;
            vl.childAlignment = TextAnchor.MiddleCenter;
            vl.childControlWidth = true; vl.childControlHeight = false;
            vl.childForceExpandWidth = false; vl.childForceExpandHeight = false;

            var tTitle = MakeLabel(mainPanel.transform, "Label_Title", "VERMIN EXTERMINATOR", 56, FontStyles.Bold);
            var tLevel = MakeLabel(mainPanel.transform, "Label_Level", "LEVEL: 1", 28);
            var tPell  = MakeLabel(mainPanel.transform, "Label_Pellets", "PELLETS: 0", 28);
            var tOpt   = MakeLabel(mainPanel.transform, "Label_Optics", "OPTICS TIER: 1", 24);
            var tStory = MakeLabel(mainPanel.transform, "Label_Story", "STORY: -", 22);
            var tDaily = MakeLabel(mainPanel.transform, "Label_Daily", "DAILY: -", 22);

            var bPlay     = MakeButton(mainPanel.transform, "Button_Play",      "PLAY");
            var bStory    = MakeButton(mainPanel.transform, "Button_PlayStory", "PLAY STORY");
            var bDaily    = MakeButton(mainPanel.transform, "Button_PlayDaily", "PLAY DAILY");
            var bShop     = MakeButton(mainPanel.transform, "Button_Shop",      "SHOP");
            var bSettings = MakeButton(mainPanel.transform, "Button_Settings",  "SETTINGS");
            var bExit     = MakeButton(mainPanel.transform, "Button_Exit",      "EXIT");

            var bCloseSet = MakeButton(settingsPanel.transform, "Button_CloseSettings", "CLOSE");
            var bCloseSetRT = bCloseSet.GetComponent<RectTransform>();
            bCloseSetRT.anchorMin = new Vector2(0.4f, 0.4f); bCloseSetRT.anchorMax = new Vector2(0.6f, 0.55f);
            bCloseSetRT.offsetMin = Vector2.zero; bCloseSetRT.offsetMax = Vector2.zero;

            BuildShop(shopPanel, out var bCloseShop, out var upgradeShop);

            var mcGo = new GameObject("MenuController");
            mcGo.transform.SetParent(canvasGo.transform, false);
            var mc = mcGo.AddComponent<MainMenuController>();
            SetField(mc, "_playButton", bPlay);
            SetField(mc, "_playStoryContractButton", bStory);
            SetField(mc, "_playDailyContractButton", bDaily);
            SetField(mc, "_settingsButton", bSettings);
            SetField(mc, "_closeSettingsButton", bCloseSet);
            SetField(mc, "_shopButton", bShop);
            SetField(mc, "_closeShopButton", bCloseShop);
            SetField(mc, "_exitButton", bExit);
            SetField(mc, "_currentLevelText", tLevel);
            SetField(mc, "_pelletsText", tPell);
            SetField(mc, "_opticsTierText", tOpt);
            SetField(mc, "_storyContractText", tStory);
            SetField(mc, "_dailyContractText", tDaily);
            SetField(mc, "_mainMenuPanel", mainPanel);
            SetField(mc, "_settingsPanel", settingsPanel);
            SetField(mc, "_shopPanel", shopPanel);

            SetField(upgradeShop, "_catalog", catalog);

            EditorSceneManager.SaveScene(scene, MAIN_MENU_SCENE);
            Debug.Log("[Vermin] MainMenu scene saved.");
        }

        public static void AddScenesToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            if (File.Exists(MAIN_MENU_SCENE)) scenes.Add(new EditorBuildSettingsScene(MAIN_MENU_SCENE, true));
            if (File.Exists(GAME_SCENE))      scenes.Add(new EditorBuildSettingsScene(GAME_SCENE, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Vermin] Build scenes updated.");
        }

        private static void BuildShop(GameObject shopPanel, out Button closeBtn, out UpgradeShopController ctrl)
        {
            ctrl = shopPanel.AddComponent<UpgradeShopController>();

            var header = MakeSubPanel(shopPanel.transform, "Header", new Vector2(0, 0.92f), new Vector2(1, 1));
            var tPell = MakeLabel(header.transform, "Label_Pellets", "PELLETS: 0", 26, FontStyles.Bold);
            Place(tPell.rectTransform, new Vector2(0.02f, 0), new Vector2(0.3f, 1));
            var tStat = MakeLabel(header.transform, "Label_Status", "", 22);
            Place(tStat.rectTransform, new Vector2(0.32f, 0), new Vector2(0.85f, 1));
            closeBtn = MakeButton(header.transform, "Button_CloseShop", "CLOSE");
            Place(closeBtn.GetComponent<RectTransform>(), new Vector2(0.87f, 0.15f), new Vector2(0.98f, 0.85f));

            var tabs = MakeSubPanel(shopPanel.transform, "CategoryTabs", new Vector2(0, 0.84f), new Vector2(1, 0.92f));
            var hl = tabs.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(16,16,8,8); hl.spacing = 8;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = false; hl.childForceExpandWidth = false;

            var scroll = new GameObject("ItemScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scroll.transform.SetParent(shopPanel.transform, false);
            Place(scroll.GetComponent<RectTransform>(), new Vector2(0, 0.08f), new Vector2(0.45f, 0.83f));
            scroll.GetComponent<Image>().color = new Color(0,0,0,0.2f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scroll.transform, false);
            Place(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0,1); crt.anchorMax = new Vector2(1,1);
            crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 100);
            var cvl = content.GetComponent<VerticalLayoutGroup>();
            cvl.padding = new RectOffset(8,8,8,8); cvl.spacing = 6;
            cvl.childControlHeight = false; cvl.childForceExpandHeight = false;
            cvl.childControlWidth = true; cvl.childForceExpandWidth = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scroll.GetComponent<ScrollRect>();
            sr.viewport = viewport.GetComponent<RectTransform>();
            sr.content = crt; sr.horizontal = false; sr.vertical = true;

            var detail = MakeSubPanel(shopPanel.transform, "DetailPanel", new Vector2(0.47f, 0.08f), new Vector2(1, 0.83f));
            var dvl = detail.AddComponent<VerticalLayoutGroup>();
            dvl.padding = new RectOffset(20,20,20,20); dvl.spacing = 8;
            dvl.childForceExpandHeight = false; dvl.childControlHeight = true;
            dvl.childControlWidth = true; dvl.childForceExpandWidth = true;

            var dn = MakeLabel(detail.transform, "DetailName", "- Select a part -", 28, FontStyles.Bold);
            var dr = MakeLabel(detail.transform, "DetailRarity", "", 24);
            var dd = MakeLabel(detail.transform, "DetailDescription", "", 20);
            var dreq = MakeLabel(detail.transform, "DetailRequirements", "", 18);
            var dsc = MakeLabel(detail.transform, "StatsCurrent", "Current loadout", 18);
            var dsw = MakeLabel(detail.transform, "StatsWith", "With this part", 18);
            var bBuy = MakeButton(detail.transform, "Button_Buy", "BUY");
            var bEq  = MakeButton(detail.transform, "Button_Equip", "EQUIP");

            var itemPrefab = MakeShopItemPrefab();
            var tabPrefab  = MakeCategoryTabPrefab();

            var empty = MakeLabel(shopPanel.transform, "EmptyState", "No parts in this category yet.", 22);
            empty.alignment = TextAlignmentOptions.Center;
            Place(empty.rectTransform, new Vector2(0.1f, 0.3f), new Vector2(0.45f, 0.6f));
            empty.gameObject.SetActive(false);

            SetField(ctrl, "_pelletsText", tPell);
            SetField(ctrl, "_statusText", tStat);
            SetField(ctrl, "_closeButton", closeBtn);
            SetField(ctrl, "_categoryTabsContainer", tabs.transform);
            SetField(ctrl, "_categoryTabPrefab", tabPrefab);
            SetField(ctrl, "_itemListContainer", content.transform);
            SetField(ctrl, "_itemButtonPrefab", itemPrefab);
            SetField(ctrl, "_detailName", dn);
            SetField(ctrl, "_detailRarity", dr);
            SetField(ctrl, "_detailDescription", dd);
            SetField(ctrl, "_detailRequirements", dreq);
            SetField(ctrl, "_detailStatsCurrent", dsc);
            SetField(ctrl, "_detailStatsWith", dsw);
            SetField(ctrl, "_buyButton", bBuy);
            SetField(ctrl, "_equipButton", bEq);
            SetField(ctrl, "_buyButtonLabel", bBuy.GetComponentInChildren<TextMeshProUGUI>());
            SetField(ctrl, "_equipButtonLabel", bEq.GetComponentInChildren<TextMeshProUGUI>());
            SetField(ctrl, "_emptyStateLabel", empty.gameObject);
        }

        private static Button MakeCategoryTabPrefab()
        {
            EnsureFolder("Assets/Prefabs"); EnsureFolder("Assets/Prefabs/UI");
            var path = "Assets/Prefabs/UI/CategoryTabButton.prefab";
            if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Button>();
            var go = new GameObject("CategoryTabButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 50);
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);
            var lab = MakeLabel(go.transform, "Label", "CATEGORY", 18, FontStyles.Bold);
            lab.alignment = TextAlignmentOptions.Center;
            Place(lab.rectTransform, Vector2.zero, Vector2.one);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved.GetComponent<Button>();
        }

        private static ShopItemButton MakeShopItemPrefab()
        {
            EnsureFolder("Assets/Prefabs"); EnsureFolder("Assets/Prefabs/UI");
            var path = "Assets/Prefabs/UI/ShopItemRow.prefab";
            if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<ShopItemButton>();

            var go = new GameObject("ShopItemRow", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
            go.GetComponent<LayoutElement>().preferredHeight = 64;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(go.transform, false);
            Place(icon.GetComponent<RectTransform>(), new Vector2(0.01f, 0.1f), new Vector2(0.12f, 0.9f));

            var n = MakeLabel(go.transform, "Label_Name", "Name", 18, FontStyles.Bold);
            Place(n.rectTransform, new Vector2(0.14f, 0.55f), new Vector2(0.7f, 0.95f));
            var s = MakeLabel(go.transform, "Label_Stats", "", 14);
            Place(s.rectTransform, new Vector2(0.14f, 0.05f), new Vector2(0.7f, 0.5f));
            var c = MakeLabel(go.transform, "Label_Cost", "0 P", 18, FontStyles.Bold);
            Place(c.rectTransform, new Vector2(0.72f, 0.1f), new Vector2(0.98f, 0.9f));
            c.alignment = TextAlignmentOptions.MidlineRight;
            var r = MakeLabel(go.transform, "Label_Rarity", "", 12);
            Place(r.rectTransform, new Vector2(0.72f, 0.55f), new Vector2(0.98f, 0.78f));
            r.alignment = TextAlignmentOptions.MidlineRight;

            var lockIcon  = MakeBadge(go.transform, "Lock_Icon",      new Color(0.9f, 0.3f, 0.3f, 0.7f));
            var ownedBadge= MakeBadge(go.transform, "Owned_Badge",    new Color(0.4f, 0.9f, 0.4f, 0.7f));
            var eqBadge   = MakeBadge(go.transform, "Equipped_Badge", new Color(1f, 0.85f, 0.25f, 0.9f));
            var newBadge  = MakeBadge(go.transform, "New_Badge",      new Color(0.3f, 0.7f, 1f, 0.9f));

            var sib = go.AddComponent<ShopItemButton>();
            SetField(sib, "_button", go.GetComponent<Button>());
            SetField(sib, "_iconImage", icon.GetComponent<Image>());
            SetField(sib, "_nameText", n);
            SetField(sib, "_costText", c);
            SetField(sib, "_rarityText", r);
            SetField(sib, "_shortStatsText", s);
            SetField(sib, "_lockIcon", lockIcon);
            SetField(sib, "_ownedBadge", ownedBadge);
            SetField(sib, "_equippedBadge", eqBadge);
            SetField(sib, "_newBadge", newBadge);

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved.GetComponent<ShopItemButton>();
        }

        private static GameObject MakeBadge(Transform parent, string name, Color col)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = col;
            Place(go.GetComponent<RectTransform>(), new Vector2(0.8f, 0.78f), new Vector2(0.88f, 0.95f));
            go.SetActive(false);
            return go;
        }

        private static OpticsTierConfig.OpticsTier MakeTier(string name, float zoom, float fov,
            ScopeOverlayController.ScopeMode defMode, float sway, float cone,
            ScopeOverlayController.ScopeMode[] supported, string desc)
        {
            return new OpticsTierConfig.OpticsTier {
                tierName = name, description = desc,
                zoomLevel = zoom, FieldOfView = fov,
                defaultScopeMode = defMode, swayMultiplier = sway,
                aimAssistConeMultiplier = cone,
                supportedScopeModes = new List<ScopeOverlayController.ScopeMode>(supported)
            };
        }

        private static WeaponPartDefinition MakePart(string id, string name, WeaponPartCategory cat,
            int cost, int level, WeaponPartRarity rar, string desc,
            int opticsTierIndex = -1, float fireRate = 1f, float recoil = 1f, float sway = 1f, float noise = 1f)
        {
            return new WeaponPartDefinition {
                id = id, displayName = name, description = desc,
                category = cat, rarity = rar, costPellets = cost,
                requiredPlayerLevel = level, opticsTierIndex = opticsTierIndex,
                modifiers = new WeaponStatModifiers {
                    fireRateMultiplier = fireRate, recoilAmountMultiplier = recoil,
                    swayMultiplier = sway, noiseRadiusMultiplier = noise
                }
            };
        }

        private static ContractDefinition GetOrCreateContract(string path, string id, string name, string desc, int level, int reward)
        {
            var c = AssetDatabase.LoadAssetAtPath<ContractDefinition>(path);
            if (c == null) { c = ScriptableObject.CreateInstance<ContractDefinition>(); AssetDatabase.CreateAsset(c, path); }
            c.contractId = id; c.displayName = name; c.description = desc;
            c.requiredPlayerLevel = level; c.basePelletReward = reward;
            EditorUtility.SetDirty(c);
            return c;
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        private static GameObject MakePanel(Transform parent, string name, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            go.GetComponent<Image>().color = bg;
            return go;
        }

        private static GameObject MakeSubPanel(Transform parent, string name, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), aMin, aMax);
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.15f);
            return go;
        }

        private static TextMeshProUGUI MakeLabel(Transform parent, string name, string text, int size, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.fontStyle = style;
            t.color = Color.white; t.alignment = TextAlignmentOptions.MidlineLeft;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = size + 10;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 56; le.preferredWidth = 320;
            var t = MakeLabel(go.transform, "Label", label, 24, FontStyles.Bold);
            t.alignment = TextAlignmentOptions.Center;
            Place(t.rectTransform, Vector2.zero, Vector2.one);
            return go.GetComponent<Button>();
        }

        private static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var t = target.GetType();
            while (t != null)
            {
                var f = t.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (f != null) {
                    f.SetValue(target, value);
                    if (target is Object uo) EditorUtility.SetDirty(uo);
                    return;
                }
                t = t.BaseType;
            }
            Debug.LogWarning($"[Vermin] Field '{fieldName}' not found on {target.GetType().Name}.");
        }
    }
}
#endif
