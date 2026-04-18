using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarnSwarmSniper.Weapon
{
    public enum WeaponPartCategory
    {
        Scope,
        Barrel,
        Magazine,
        Trigger,
        Stock,
        Accessory
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
    }

    [Serializable]
    public class WeaponPartDefinition
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public WeaponPartCategory category;

        [Header("Progression")]
        public int costPellets;
        public int requiredPlayerLevel = 1;
        public string[] requiresPartIds;

        [Header("Effects")]
        public WeaponStatModifiers modifiers = new WeaponStatModifiers();
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
    }
}

