using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BarnSwarmSniper.Game;
using BarnSwarmSniper.Data;

namespace BarnSwarmSniper.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _playStoryContractButton;
        [SerializeField] private Button _playDailyContractButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _closeSettingsButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private TextMeshProUGUI _currentLevelText;
        [SerializeField] private TextMeshProUGUI _pelletsText;
        [SerializeField] private TextMeshProUGUI _opticsTierText;
        [SerializeField] private TextMeshProUGUI _storyContractText;
        [SerializeField] private TextMeshProUGUI _dailyContractText;

        [Header("Panels")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _settingsPanel;

        [Header("Settings UI (Optional)")]
        [SerializeField] private Toggle _breathingSwayToggle;

        private GameManager _gameManager;
        private PlayerProgressManager _playerProgressManager;
        private SettingsManager _settingsManager;

        private void Awake()
        {
            _playButton.onClick.AddListener(OnPlayButtonClicked);
            if (_playStoryContractButton != null) _playStoryContractButton.onClick.AddListener(OnPlayStoryContractClicked);
            if (_playDailyContractButton != null) _playDailyContractButton.onClick.AddListener(OnPlayDailyContractClicked);
            _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            if (_closeSettingsButton != null)
            {
                _closeSettingsButton.onClick.AddListener(ShowMainMenu);
            }
            _exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        private void Start()
        {
            ResolveManagers();
            UpdateMainMenuUI();
            ShowMainMenu();
        }

        private void OnEnable()
        {
            ResolveManagers();
            UpdateMainMenuUI();
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(OnPlayButtonClicked);
            if (_playStoryContractButton != null) _playStoryContractButton.onClick.RemoveListener(OnPlayStoryContractClicked);
            if (_playDailyContractButton != null) _playDailyContractButton.onClick.RemoveListener(OnPlayDailyContractClicked);
            _settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            if (_closeSettingsButton != null)
            {
                _closeSettingsButton.onClick.RemoveListener(ShowMainMenu);
            }
            _exitButton.onClick.RemoveListener(OnExitButtonClicked);
        }

        private void UpdateMainMenuUI()
        {
            if (_playerProgressManager != null && _playerProgressManager.CurrentProgress != null)
            {
                _currentLevelText.text = $"LEVEL: {_playerProgressManager.CurrentProgress.playerLevel}";
                _pelletsText.text = $"PELLETS: {_playerProgressManager.CurrentProgress.pelletsOwned}";
                _opticsTierText.text = $"OPTICS TIER: {_playerProgressManager.CurrentProgress.opticsTierUnlocked + 1}";
            }

            if (_gameManager != null && _gameManager.ContractManager != null)
            {
                _gameManager.ContractManager.RefreshAvailableContracts();
                var story = _gameManager.ContractManager.ActiveStoryContract;
                var daily = _gameManager.ContractManager.ActiveDailyContract;
                if (_storyContractText != null)
                {
                    _storyContractText.text = story != null
                        ? $"STORY: {story.displayName} (+{story.basePelletReward} pellets)"
                        : "STORY: No eligible contract";
                }

                if (_dailyContractText != null)
                {
                    _dailyContractText.text = daily != null
                        ? $"DAILY: {daily.displayName} (+{daily.basePelletReward} pellets)"
                        : "DAILY: No eligible contract";
                }
            }

            if (_settingsManager != null && _settingsManager.CurrentSettings != null && _breathingSwayToggle != null)
            {
                _breathingSwayToggle.isOn = _settingsManager.CurrentSettings.BreathingSwayEnabled;
            }
        }

        private void OnPlayButtonClicked()
        {
            if (_gameManager == null)
            {
                Debug.LogError("MainMenuController: GameManager reference missing.");
                return;
            }

            _gameManager.StartGame();
        }

        private void OnPlayStoryContractClicked()
        {
            if (_gameManager == null)
            {
                Debug.LogError("MainMenuController: GameManager reference missing.");
                return;
            }

            _gameManager.StartStoryContractGame();
        }

        private void OnPlayDailyContractClicked()
        {
            if (_gameManager == null)
            {
                Debug.LogError("MainMenuController: GameManager reference missing.");
                return;
            }

            _gameManager.StartDailyContractGame();
        }

        private void OnSettingsButtonClicked()
        {
            ShowSettingsMenu();
        }

        private void OnExitButtonClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void ShowMainMenu()
        {
            if (_mainMenuPanel != null)
            {
                _mainMenuPanel.SetActive(true);
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }

            UpdateMainMenuUI();
        }

        public void ShowSettingsMenu()
        {
            if (_mainMenuPanel != null)
            {
                _mainMenuPanel.SetActive(false);
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(true);
            }
        }

        public void OnBreathingSwayToggleChanged(bool enabled)
        {
            ResolveManagers();
            if (_settingsManager != null)
            {
                _settingsManager.UpdateBreathingSway(enabled);
            }
        }

        private void ResolveManagers()
        {
            _gameManager = GameManager.Instance != null
                ? GameManager.Instance
                : FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

            if (_gameManager == null)
            {
                Debug.LogError("MainMenuController: GameManager instance not found in scene.");
                return;
            }

            _playerProgressManager = _gameManager.PlayerProgressManager;
            _settingsManager = _gameManager.SettingsManager;
        }
    }
}
