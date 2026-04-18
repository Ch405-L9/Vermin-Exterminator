using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarnSwarmSniper.Weapon
{
    public enum WeaponPartCategory
    {
        // Original categories (kept for backward compatibility with existing assets)
        Scope,
        Barrel,
        Magazine,
        Trigger,
        Stock,
        Accessory,
        // Phase 1 additions
        Rifle,
        Ammo,
        Cosmetic
    }

    public enum WeaponPartRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public class WeaponStatModifiers
    {
        [Header("Core")]
        public float fireRateMultiplier = 1f;

        [Header("Recoil")]
        public float recoilAmountMultiplier = 1f;
        public float recoilRecoveryMultiplier = 1f;

        [Header("Aim Assist (applies to SettingsData consumers)")]
        public float aimAssistFrictionStrengthMultiplier = 1f;
        public float aimAssistFrictionRadiusMultiplier = 1f;

        [Header("Optics")]
        public int maxOpticsTierDelta = 0; // increases allowed tier index (clamped)
        public float zoomMultiplier = 1f;  // multiplies effective zoom / divides FOV

        [Header("Handling")]
        public float swayMultiplier = 1f;          // lower = steadier
        public float noiseRadiusMultiplier = 1f;   // lower = quieter (alerts fewer rats)

        [Header("Magazine / Capacity")]
        public int magazineSizeDelta = 0;          // additive bonus
    }

    [Serializable]
    public class WeaponPartDefinition
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public WeaponPartCategory category;
        public WeaponPartRarity rarity = WeaponPartRarity.Common;
        public Sprite icon;

        [Header("Progression")]
        public int costPellets;
        public int requiredPlayerLevel = 1;
        public string[] requiresPartIds;
        public string[] requiresContractIds; // optional: contract IDs that must have been completed

        [Header("Optics (Scope parts only)")]
        [Tooltip("For Scope parts, the OpticsTierConfig index this part unlocks/represents. -1 = n/a.")]
        public int opticsTierIndex = -1;

        [Header("Effects")]
        public WeaponStatModifiers modifiers = new WeaponStatModifiers();

        /// <summary>Short one-line stat preview string for list display.</summary>
        public string GetShortStatLine()
        {
            if (modifiers == null) return "";
            var parts = new List<string>(6);
            if (!Mathf.Approximately(modifiers.fireRateMultiplier, 1f))
                parts.Add($"Rate x{modifiers.fireRateMultiplier:0.00}");
            if (!Mathf.Approximately(modifiers.recoilAmountMultiplier, 1f))
                parts.Add($"Recoil x{modifiers.recoilAmountMultiplier:0.00}");
            if (!Mathf.Approximately(modifiers.swayMultiplier, 1f))
                parts.Add($"Sway x{modifiers.swayMultiplier:0.00}");
            if (!Mathf.Approximately(modifiers.noiseRadiusMultiplier, 1f))
                parts.Add($"Noise x{modifiers.noiseRadiusMultiplier:0.00}");
            if (modifiers.magazineSizeDelta != 0)
                parts.Add((modifiers.magazineSizeDelta > 0 ? "+" : "") + modifiers.magazineSizeDelta + " mag");
            if (modifiers.maxOpticsTierDelta != 0)
                parts.Add((modifiers.maxOpticsTierDelta > 0 ? "+" : "") + modifiers.maxOpticsTierDelta + " optics tier");
            return string.Join(" | ", parts);
        }
    }

    [CreateAssetMenu(fileName = "WeaponPartCatalog", menuName = "BarnSwarmSniper/Weapon Part Catalog", order = 10)]
    public class WeaponPartCatalog : ScriptableObject
    {
        public List<WeaponPartDefinition> parts = new List<WeaponPartDefinition>();

        public bool TryGetPart(string id, out WeaponPartDefinition part)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                part = null;
                return false;
            }

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null && parts[i].id == id)
                {
                    part = parts[i];
                    return true;
                }
            }

            part = null;
            return false;
        }

        public IEnumerable<WeaponPartDefinition> GetPartsByCategory(WeaponPartCategory category)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (p != null && p.category == category)
                {
                    yield return p;
                }
            }
        }

        /// <summary>Returns all distinct categories that have at least one part defined.</summary>
        public IEnumerable<WeaponPartCategory> GetPopulatedCategories()
        {
            var seen = new HashSet<WeaponPartCategory>();
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (p == null) continue;
                if (seen.Add(p.category))
                {
                    yield return p.category;
                }
            }
        }
    }
}
