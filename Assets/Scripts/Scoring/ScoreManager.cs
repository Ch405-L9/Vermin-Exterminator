using UnityEngine;
using BarnSwarmSniper.AI;

namespace BarnSwarmSniper.Scoring
{
    public class ScoreManager : MonoBehaviour
    {
        public int CurrentScore { get; private set; }
        public int KillCount { get; private set; }
        public int MissCount { get; private set; }

        public event System.Action<int> OnScoreChanged;
        public event System.Action<int> OnKillCountChanged;
        public event System.Action OnMiss;

        void Start()
        {
            ResetScore();
        }

        public void AddScore(int baseScore, float multiplier = 1f)
        {
            int scoreToAdd = Mathf.RoundToInt(baseScore * multiplier);
            CurrentScore += scoreToAdd;
            OnScoreChanged?.Invoke(CurrentScore);
        }

        public void NotifyMiss()
        {
            MissCount++;
            OnMiss?.Invoke();
        }

        public void ResetScore()
        {
            CurrentScore = 0;
            KillCount = 0;
            MissCount = 0;
            OnScoreChanged?.Invoke(CurrentScore);
            OnKillCountChanged?.Invoke(KillCount);
        }

        // Example method for calculating score based on rat kill
        public void OnRatKilled(RatFSM rat)
        {
            // Implement scoring logic here: base score, headshot, distance, speed kill, combo multiplier
            int baseScore = 100; // Example base score
            float multiplier = 1.0f; // Placeholder for various multipliers

            // TODO: Add logic for headshot, distance, speed kill, combo

            AddScore(baseScore, multiplier);
            KillCount++;
            OnKillCountChanged?.Invoke(KillCount);
        }
    }
}
