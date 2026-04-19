using System.Collections.Generic;
using BarnSwarmSniper.Weapon;

namespace BarnSwarmSniper.Data
{
    /// <summary>
    /// Immutable snapshot of the player's active loadout for a mission.
    /// Built by GameManager before a contract starts and consumed by
    /// WeaponController / optics systems / RuntimeSettings. Never mutates
    /// the underlying ScriptableObjects.
    /// </summary>
    public class RuntimeLoadout
    {
        public int PlayerLevel { get; private set; } = 1;
        public int OpticsTierUnlocked { get; private set; }
        public string SelectedAmmoId { get; private set; } = string.Empty;

        /// <summary>Aggregated modifiers from all equipped parts (safe copy).</summary>
        public WeaponStatModifiers Aggregate { get; private set; } = new WeaponStatModifiers();

        /// <summary>Resolved part definitions per equipped slot (category -> part).</summary>
        public IReadOnlyDictionary<WeaponPartCategory, WeaponPartDefinition> EquippedParts => _equippedParts;
        private readonly Dictionary<WeaponPartCategory, WeaponPartDefinition> _equippedParts = new();

        public static RuntimeLoadout Build(PlayerProgress progress, WeaponPartCatalog catalog)
        {
            var loadout = new RuntimeLoadout();
            if (progress == null)
            {
                return loadout;
            }

            loadout.PlayerLevel = progress.playerLevel;
            loadout.OpticsTierUnlocked = progress.opticsTierUnlocked;
            loadout.SelectedAmmoId = progress.selectedAmmoId ?? string.Empty;

            if (catalog == null || progress.equippedParts == null)
            {
                return loadout;
            }

            var aggregate = new WeaponStatModifiers();
            for (int i = 0; i < progress.equippedParts.Length; i++)
            {
                var slot = progress.equippedParts[i];
                if (slot == null || string.IsNullOrWhiteSpace(slot.partId))
                {
                    continue;
                }

                if (!catalog.TryGetPart(slot.partId, out var part) || part == null)
                {
                    continue;
                }

                // Safety: only keep parts the player owns.
                if (!progress.OwnsPart(part.id))
                {
                    continue;
                }

                loadout._equippedParts[part.category] = part;

                var m = part.modifiers;
                if (m == null) continue;

                aggregate.fireRateMultiplier *= m.fireRateMultiplier;
                aggregate.recoilAmountMultiplier *= m.recoilAmountMultiplier;
                aggregate.recoilRecoveryMultiplier *= m.recoilRecoveryMultiplier;
                aggregate.aimAssistFrictionStrengthMultiplier *= m.aimAssistFrictionStrengthMultiplier;
                aggregate.aimAssistFrictionRadiusMultiplier *= m.aimAssistFrictionRadiusMultiplier;
                aggregate.maxOpticsTierDelta += m.maxOpticsTierDelta;
                aggregate.zoomMultiplier *= m.zoomMultiplier;
                aggregate.swayMultiplier *= m.swayMultiplier;
                aggregate.noiseRadiusMultiplier *= m.noiseRadiusMultiplier;
                aggregate.magazineSizeDelta += m.magazineSizeDelta;
            }

            loadout.Aggregate = aggregate;
            return loadout;
        }
    }
}
