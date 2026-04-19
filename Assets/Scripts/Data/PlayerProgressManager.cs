using UnityEngine;
using System.IO;

namespace BarnSwarmSniper.Data
{
    public class PlayerProgressManager : MonoBehaviour
    {
        private string _savePath;
        public PlayerProgress CurrentProgress { get; private set; }

        void Awake()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "playerProgress.json");
            LoadProgress();
        }

        public void LoadProgress()
        {
            if (File.Exists(_savePath))
            {
                string json = File.ReadAllText(_savePath);
                CurrentProgress = JsonUtility.FromJson<PlayerProgress>(json);
                Debug.Log("Player progress loaded.");
            }
            else
            {
                CurrentProgress = new PlayerProgress();
                Debug.Log("No player progress found, creating new one.");
                SaveProgress(); // Save the newly created progress
            }
        }

        public void SaveProgress()
        {
            string json = JsonUtility.ToJson(CurrentProgress);
            File.WriteAllText(_savePath, json);
            Debug.Log("Player progress saved.");
        }

        public void UpdatePellets(int newPellets)
        {
            CurrentProgress.pelletsOwned = newPellets;
            SaveProgress();
        }

        public void UnlockOpticsTier(int tierIndex)
        {
            if (tierIndex > CurrentProgress.opticsTierUnlocked)
            {
                CurrentProgress.opticsTierUnlocked = tierIndex;
                SaveProgress();
            }
        }

        public void UnlockBarnVariant(int variantIndex)
        {
            // Check if already unlocked
            foreach (int unlockedVariant in CurrentProgress.barnVariantsUnlocked)
            {
                if (unlockedVariant == variantIndex) return;
            }

            // Add to unlocked variants
            int[] newVariants = new int[CurrentProgress.barnVariantsUnlocked.Length + 1];
            CurrentProgress.barnVariantsUnlocked.CopyTo(newVariants, 0);
            newVariants[newVariants.Length - 1] = variantIndex;
            CurrentProgress.barnVariantsUnlocked = newVariants;
            SaveProgress();
        }
    }
}
