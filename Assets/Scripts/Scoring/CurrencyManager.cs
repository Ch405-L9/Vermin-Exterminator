using UnityEngine;

namespace BarnSwarmSniper.Scoring
{
    public class CurrencyManager : MonoBehaviour
    {
        public int PelletsOwned { get; private set; }

        public event System.Action<int> OnPelletsChanged;

        void Start()
        {
            PelletsOwned = 0;
        }

        public void AddPellets(int amount)
        {
            PelletsOwned += amount;
            OnPelletsChanged?.Invoke(PelletsOwned);
        }

        public bool TrySpendPellets(int amount)
        {
            if (PelletsOwned >= amount)
            {
                PelletsOwned -= amount;
                OnPelletsChanged?.Invoke(PelletsOwned);
                return true;
            }
            return false;
        }

        public void InitializePellets(int pellets)
        {
            PelletsOwned = Mathf.Max(0, pellets);
            OnPelletsChanged?.Invoke(PelletsOwned);
        }

        public void ConvertScoreToPellets(int score)
        {
            // Simple conversion for now, e.g., 100 score = 1 pellet
            int pelletsEarned = score / 100;
            AddPellets(pelletsEarned);
            Debug.Log($"Converted {score} score to {pelletsEarned} pellets.");
        }
    }
}
