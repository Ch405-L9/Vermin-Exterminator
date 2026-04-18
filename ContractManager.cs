using System;
using System.Collections.Generic;
using BarnSwarmSniper.Data;
using BarnSwarmSniper.Scoring;
using UnityEngine;

namespace BarnSwarmSniper.Game
{
    public class ContractManager : MonoBehaviour
    {
        [Header("Contract Sources")]
        [SerializeField] private ContractSet _storyContracts;
        [SerializeField] private ContractSet _dailyContracts;

        [Header("Dependencies")]
        [SerializeField] private PlayerProgressManager _playerProgressManager;
        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private CurrencyManager _currencyManager;

        public ContractDefinition SelectedContract { get; private set; }
        public ContractDefinition ActiveStoryContract { get; private set; }
        public ContractDefinition ActiveDailyContract { get; private set; }
        public bool IsContractActive { get; private set; }
        public float ElapsedContractTimeSeconds => _elapsedSeconds;

        public event Action<ContractDefinition> OnSelectedContractChanged;
        public event Action<int> OnRewardGranted;

        private float _elapsedSeconds;
        private int _scoreAtEnd;
        private int _killAtEnd;
        private int _missAtEnd;
        private readonly HashSet<string> _completedChallenges = new();

        private void Awake()
        {
            ResolveDependencies();
            RefreshAvailableContracts();
            BindScoreEvents();
        }

        private void OnDestroy()
        {
            UnbindScoreEvents();
        }

        public void RefreshAvailableContracts()
        {
            int playerLevel = _playerProgressManager?.CurrentProgress?.playerLevel ?? 1;
            ActiveStoryContract = SelectFirstAllowed(_storyContracts, playerLevel);
            ActiveDailyContract = SelectDailyAllowed(_dailyContracts, playerLevel, DateTime.UtcNow.Date);
        }

        public bool SelectStoryContract()
        {
            RefreshAvailableContracts();
            if (ActiveStoryContract == null)
            {
                return false;
            }

            SelectedContract = ActiveStoryContract;
            OnSelectedContractChanged?.Invoke(SelectedContract);
            return true;
        }

        public bool SelectDailyContract()
        {
            RefreshAvailableContracts();
            if (ActiveDailyContract == null)
            {
                return false;
            }

            SelectedContract = ActiveDailyContract;
            OnSelectedContractChanged?.Invoke(SelectedContract);
            return true;
        }

        public void StartSelectedContract()
        {
            if (SelectedContract == null)
            {
                return;
            }

            IsContractActive = true;
            _elapsedSeconds = 0f;
            _scoreAtEnd = 0;
            _killAtEnd = 0;
            _missAtEnd = 0;
            _completedChallenges.Clear();
        }

        public void Tick(float deltaSeconds)
        {
            if (!IsContractActive)
            {
                return;
            }

            _elapsedSeconds += Mathf.Max(0f, deltaSeconds);
        }

        public int EndContractAndGrantReward(bool success)
        {
            if (!IsContractActive || SelectedContract == null)
            {
                return 0;
            }

            IsContractActive = false;

            _scoreAtEnd = _scoreManager != null ? _scoreManager.CurrentScore : 0;
            _killAtEnd = _scoreManager != null ? _scoreManager.KillCount : 0;
            _missAtEnd = _scoreManager != null ? _scoreManager.MissCount : 0;

            if (!success)
            {
                return 0;
            }

            int reward = SelectedContract.basePelletReward;
            if (SelectedContract.challenges != null)
            {
                for (int i = 0; i < SelectedContract.challenges.Count; i++)
                {
                    var challenge = SelectedContract.challenges[i];
                    if (challenge == null)
                    {
                        continue;
                    }

                    if (IsChallengeComplete(challenge))
                    {
                        _completedChallenges.Add(challenge.challengeId);
                        reward += challenge.bonusPelletReward;
                    }
                }
            }

            if (_currencyManager != null)
            {
                _currencyManager.AddPellets(reward);
            }

            if (_playerProgressManager?.CurrentProgress != null && _currencyManager != null)
            {
                _playerProgressManager.UpdatePellets(_currencyManager.PelletsOwned);
                _playerProgressManager.SaveProgress();
            }

            OnRewardGranted?.Invoke(reward);
            return reward;
        }

        public bool IsChallengeMarkedComplete(string challengeId)
        {
            return _completedChallenges.Contains(challengeId);
        }

        private bool IsChallengeComplete(ContractChallenge challenge)
        {
            return challenge.type switch
            {
                ChallengeType.NoMiss => _missAtEnd <= 0,
                ChallengeType.TimeLimit => _elapsedSeconds <= challenge.thresholdValue,
                ChallengeType.MinScore => _scoreAtEnd >= Mathf.RoundToInt(challenge.thresholdValue),
                ChallengeType.MinKills => _killAtEnd >= Mathf.RoundToInt(challenge.thresholdValue),
                ChallengeType.AmmoLimit => true, // ammo system not integrated yet
                ChallengeType.MinHeadshotPercent => false, // headshot telemetry not integrated yet
                _ => false
            };
        }

        private static ContractDefinition SelectFirstAllowed(ContractSet set, int playerLevel)
        {
            if (set == null || set.contracts == null)
            {
                return null;
            }

            for (int i = 0; i < set.contracts.Count; i++)
            {
                var c = set.contracts[i];
                if (c != null && c.requiredPlayerLevel <= playerLevel)
                {
                    return c;
                }
            }

            return null;
        }

        private static ContractDefinition SelectDailyAllowed(ContractSet set, int playerLevel, DateTime date)
        {
            if (set == null || set.contracts == null || set.contracts.Count == 0)
            {
                return null;
            }

            var allowed = new List<ContractDefinition>();
            for (int i = 0; i < set.contracts.Count; i++)
            {
                var c = set.contracts[i];
                if (c != null && c.requiredPlayerLevel <= playerLevel)
                {
                    allowed.Add(c);
                }
            }

            if (allowed.Count == 0)
            {
                return null;
            }

            int idx = Mathf.Abs(date.GetHashCode()) % allowed.Count;
            return allowed[idx];
        }

        private void ResolveDependencies()
        {
            if (_playerProgressManager == null)
            {
                _playerProgressManager = FindFirstObjectByType<PlayerProgressManager>(FindObjectsInactive.Include);
            }

            if (_scoreManager == null)
            {
                _scoreManager = FindFirstObjectByType<ScoreManager>(FindObjectsInactive.Include);
            }

            if (_currencyManager == null)
            {
                _currencyManager = FindFirstObjectByType<CurrencyManager>(FindObjectsInactive.Include);
            }
        }

        private void BindScoreEvents()
        {
            if (_scoreManager == null)
            {
                return;
            }

            _scoreManager.OnScoreChanged += HandleScoreChanged;
            _scoreManager.OnMiss += HandleMiss;
            _scoreManager.OnKillCountChanged += HandleKillChanged;
        }

        private void UnbindScoreEvents()
        {
            if (_scoreManager == null)
            {
                return;
            }

            _scoreManager.OnScoreChanged -= HandleScoreChanged;
            _scoreManager.OnMiss -= HandleMiss;
            _scoreManager.OnKillCountChanged -= HandleKillChanged;
        }

        private void HandleScoreChanged(int score) { }
        private void HandleMiss() { }
        private void HandleKillChanged(int kills) { }
    }
}

