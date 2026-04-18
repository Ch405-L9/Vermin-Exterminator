using System;
using BarnSwarmSniper.AI;
using BarnSwarmSniper.Data;
using BarnSwarmSniper.Level;
using BarnSwarmSniper.Scoring;
using BarnSwarmSniper.UI;
using BarnSwarmSniper.Weapon;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BarnSwarmSniper.Game
{
    public class GameManager : MonoBehaviour
    {
        public enum GameState
        {
            MainMenu,
            Loading,
            Playing,
            Paused,
            Results
        }

        public static GameManager Instance { get; private set; }

        [Header("Persistent Managers")]
        [SerializeField] private PlayerProgressManager _playerProgressManager;
        [SerializeField] private SettingsManager _settingsManager;
        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private ContractManager _contractManager;

        [Header("Gameplay Managers (Game scene)")]
        [SerializeField] private LevelGenerator _levelGenerator;
        [SerializeField] private SpawnZoneManager _spawnZoneManager;
        [SerializeField] private RatAIManager _ratAIManager;
        [SerializeField] private WeaponController _weaponController;
        [SerializeField] private HUDController _hudController;
        [SerializeField] private WeaponPartCatalog _weaponPartCatalog;

        [Header("Game Settings")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        [SerializeField] private string _gameSceneName = "Game";
        [SerializeField] private float _levelDurationSeconds = 120f;

        public event Action<GameState> OnGameStateChanged;
        public event Action<float> OnTimerChanged;
        public event Action<int> OnScoreChanged;
        public event Action<int> OnPelletsChanged;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public float TimeRemainingSeconds => _timeRemainingSeconds;
        public PlayerProgressManager PlayerProgressManager => _playerProgressManager;
        public SettingsManager SettingsManager => _settingsManager;
        public ContractManager ContractManager => _contractManager;
        public WeaponPartCatalog WeaponPartCatalog => _weaponPartCatalog;
        public RuntimeSettings CurrentRuntimeSettings { get; private set; }
        public RuntimeLoadout CurrentRuntimeLoadout { get; private set; }

        private float _timeRemainingSeconds;
        private bool _pendingGameStart;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResolvePersistentReferences();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (_playerProgressManager != null)
            {
                _playerProgressManager.LoadProgress();
            }

            if (_settingsManager != null)
            {
                _settingsManager.LoadSettings();
                CurrentRuntimeSettings = RuntimeSettings.FromSettingsData(_settingsManager.CurrentSettings);
            }

            if (_currencyManager != null && _playerProgressManager?.CurrentProgress != null)
            {
                _currencyManager.InitializePellets(_playerProgressManager.CurrentProgress.pelletsOwned);
            }

            if (_contractManager == null)
            {
                _contractManager = FindFirstObjectByType<ContractManager>(FindObjectsInactive.Include);
            }

            _contractManager?.RefreshAvailableContracts();
            BindCommonSubscriptions();
            SetState(GameState.MainMenu);
        }

        private void Update()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            _timeRemainingSeconds = Mathf.Max(0f, _timeRemainingSeconds - Time.deltaTime);
            OnTimerChanged?.Invoke(_timeRemainingSeconds);
            _hudController?.UpdateTimer(_timeRemainingSeconds);
            _contractManager?.Tick(Time.deltaTime);

            if (_timeRemainingSeconds <= 0f)
            {
                EndLevel();
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UnbindSubscriptions();
        }

        public void StartGame()
        {
            if (CurrentState == GameState.Loading || CurrentState == GameState.Playing)
            {
                return;
            }

            if (_contractManager != null && _contractManager.SelectedContract == null)
            {
                _contractManager.SelectStoryContract();
            }

            _pendingGameStart = true;
            SetState(GameState.Loading);
            Time.timeScale = 1f;
            SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
        }

        public void StartStoryContractGame()
        {
            if (_contractManager != null && !_contractManager.SelectStoryContract())
            {
                Debug.LogWarning("No eligible story contract available, starting default game.");
            }

            StartGame();
        }

        public void StartDailyContractGame()
        {
            if (_contractManager != null && !_contractManager.SelectDailyContract())
            {
                Debug.LogWarning("No eligible daily contract available, starting default game.");
            }

            StartGame();
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void EndLevel()
        {
            if (CurrentState != GameState.Playing && CurrentState != GameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            SetState(GameState.Results);

            if (_currencyManager != null && _scoreManager != null)
            {
                _currencyManager.ConvertScoreToPellets(_scoreManager.CurrentScore);
            }

            if (_playerProgressManager?.CurrentProgress != null && _currencyManager != null)
            {
                _playerProgressManager.UpdatePellets(_currencyManager.PelletsOwned);
                _playerProgressManager.SaveProgress();
            }

            _contractManager?.EndContractAndGrantReward(true);

            // Record contract completion for shop-prerequisite gating.
            if (_contractManager?.SelectedContract != null && _playerProgressManager?.CurrentProgress != null)
            {
                var cid = _contractManager.SelectedContract.contractId;
                if (!string.IsNullOrWhiteSpace(cid))
                {
                    _playerProgressManager.CurrentProgress.MarkContractCompleted(cid);
                    _playerProgressManager.SaveProgress();
                }
            }

            SceneManager.LoadScene(_mainMenuSceneName, LoadSceneMode.Single);
            SetState(GameState.MainMenu);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResolveSceneReferences();

            if (scene.name != _gameSceneName || !_pendingGameStart)
            {
                if (scene.name == _mainMenuSceneName)
                {
                    SetState(GameState.MainMenu);
                }
                return;
            }

            _pendingGameStart = false;
            BeginGameplaySession();
        }

        private void BeginGameplaySession()
        {
            CurrentRuntimeSettings = RuntimeSettings.FromSettingsData(_settingsManager != null ? _settingsManager.CurrentSettings : null);

            if (_scoreManager != null)
            {
                _scoreManager.ResetScore();
            }

            _timeRemainingSeconds = _levelDurationSeconds;
            OnTimerChanged?.Invoke(_timeRemainingSeconds);

            ApplyEquippedLoadoutToWeapon();
            _contractManager?.StartSelectedContract();

            if (_playerProgressManager?.CurrentProgress != null && _levelGenerator != null)
            {
                _levelGenerator.GenerateLevel(
                    _playerProgressManager.CurrentProgress.playerLevel,
                    SystemInfo.deviceUniqueIdentifier);
            }

            _hudController?.UpdateTimer(_timeRemainingSeconds);
            SetState(GameState.Playing);
        }

        private void ApplyEquippedLoadoutToWeapon()
        {
            if (_weaponController == null || _playerProgressManager?.CurrentProgress == null)
            {
                return;
            }

            // Build a safe, immutable snapshot of the player's loadout for this session.
            CurrentRuntimeLoadout = RuntimeLoadout.Build(
                _playerProgressManager.CurrentProgress,
                _weaponPartCatalog);

            if (_weaponPartCatalog == null)
            {
                // No catalog assigned: leave weapon at default tuning but still keep a (mostly empty) loadout.
                return;
            }

            _weaponController.ApplyModifiers(CurrentRuntimeLoadout.Aggregate);
        }

        private void ResolvePersistentReferences()
        {
            if (_playerProgressManager == null)
            {
                _playerProgressManager = FindFirstObjectByType<PlayerProgressManager>(FindObjectsInactive.Include);
            }

            if (_settingsManager == null)
            {
                _settingsManager = FindFirstObjectByType<SettingsManager>(FindObjectsInactive.Include);
            }

            if (_scoreManager == null)
            {
                _scoreManager = FindFirstObjectByType<ScoreManager>(FindObjectsInactive.Include);
            }

            if (_currencyManager == null)
            {
                _currencyManager = FindFirstObjectByType<CurrencyManager>(FindObjectsInactive.Include);
            }

            if (_contractManager == null)
            {
                _contractManager = FindFirstObjectByType<ContractManager>(FindObjectsInactive.Include);
            }
        }

        private void ResolveSceneReferences()
        {
            _levelGenerator ??= FindFirstObjectByType<LevelGenerator>(FindObjectsInactive.Exclude);
            _spawnZoneManager ??= FindFirstObjectByType<SpawnZoneManager>(FindObjectsInactive.Exclude);
            _ratAIManager ??= FindFirstObjectByType<RatAIManager>(FindObjectsInactive.Exclude);
            _weaponController ??= FindFirstObjectByType<WeaponController>(FindObjectsInactive.Exclude);
            _hudController ??= FindFirstObjectByType<HUDController>(FindObjectsInactive.Exclude);

            if (_scoreManager == null || _currencyManager == null)
            {
                ResolvePersistentReferences();
            }

            BindCommonSubscriptions();
        }

        private void BindCommonSubscriptions()
        {
            UnbindSubscriptions();

            if (_scoreManager != null)
            {
                _scoreManager.OnScoreChanged += HandleScoreChanged;
                _scoreManager.OnMiss += HandleMiss;
            }

            if (_currencyManager != null)
            {
                _currencyManager.OnPelletsChanged += HandlePelletsChanged;
            }

            if (_ratAIManager != null && _scoreManager != null)
            {
                _ratAIManager.OnRatKilled += _scoreManager.OnRatKilled;
            }

        }

        private void UnbindSubscriptions()
        {
            if (_scoreManager != null)
            {
                _scoreManager.OnScoreChanged -= HandleScoreChanged;
                _scoreManager.OnMiss -= HandleMiss;
            }

            if (_currencyManager != null)
            {
                _currencyManager.OnPelletsChanged -= HandlePelletsChanged;
            }

            if (_ratAIManager != null && _scoreManager != null)
            {
                _ratAIManager.OnRatKilled -= _scoreManager.OnRatKilled;
            }

        }

        private void SetState(GameState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            CurrentState = nextState;
            OnGameStateChanged?.Invoke(CurrentState);
        }

        private void HandleScoreChanged(int newScore)
        {
            OnScoreChanged?.Invoke(newScore);
            _hudController?.UpdateScore(newScore);
        }

        private void HandleMiss()
        {
            Debug.Log("Missed shot.");
        }

        private void HandlePelletsChanged(int newPellets)
        {
            OnPelletsChanged?.Invoke(newPellets);
            _hudController?.UpdatePellets(newPellets);
        }
    }
}
