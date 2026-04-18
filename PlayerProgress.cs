using System;

namespace BarnSwarmSniper.Data
{
    [Serializable]
    public class PlayerProgress
    {
        public int playerLevel = 1;
        public int pelletsOwned = 0;
        public int opticsTierUnlocked = 0; // Index of the highest unlocked optics tier
        public int[] barnVariantsUnlocked = { 0 }; // Indices of unlocked barn variants

        // Upgrades / Loadout (JsonUtility-safe)
        public string[] ownedPartIds = Array.Empty<string>();
        public EquippedPart[] equippedParts = Array.Empty<EquippedPart>();

        [Serializable]
        public class EquippedPart
        {
            public string category; // stores enum name (WeaponPartCategory)
            public string partId;
        }

        // Constructor for new game
        public PlayerProgress()
        {
            playerLevel = 1;
            pelletsOwned = 0;
            opticsTierUnlocked = 0;
            barnVariantsUnlocked = new int[] { 0 };
            ownedPartIds = Array.Empty<string>();
            equippedParts = Array.Empty<EquippedPart>();
        }
    }
}
