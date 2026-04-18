using System;
using System.Collections.Generic;
using BarnSwarmSniper.Weapon;

namespace BarnSwarmSniper.Data
{
    public static class PlayerProgressUpgrades
    {
        public static bool OwnsPart(this PlayerProgress progress, string partId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(partId) || progress.ownedPartIds == null)
            {
                return false;
            }

            for (int i = 0; i < progress.ownedPartIds.Length; i++)
            {
                if (progress.ownedPartIds[i] == partId)
                {
                    return true;
                }
            }

            return false;
        }

        public static void AddOwnedPart(this PlayerProgress progress, string partId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(partId))
            {
                return;
            }

            if (progress.OwnsPart(partId))
            {
                return;
            }

            var old = progress.ownedPartIds ?? Array.Empty<string>();
            var next = new string[old.Length + 1];
            Array.Copy(old, next, old.Length);
            next[next.Length - 1] = partId;
            progress.ownedPartIds = next;
        }

        public static string GetEquippedPartId(this PlayerProgress progress, WeaponPartCategory category)
        {
            if (progress == null || progress.equippedParts == null)
            {
                return null;
            }

            var cat = category.ToString();
            for (int i = 0; i < progress.equippedParts.Length; i++)
            {
                var slot = progress.equippedParts[i];
                if (slot != null && slot.category == cat)
                {
                    return slot.partId;
                }
            }

            return null;
        }

        public static void EquipPart(this PlayerProgress progress, WeaponPartCategory category, string partId)
        {
            if (progress == null)
            {
                return;
            }

            var cat = category.ToString();
            var list = new List<PlayerProgress.EquippedPart>(progress.equippedParts ?? Array.Empty<PlayerProgress.EquippedPart>());

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].category == cat)
                {
                    list[i].partId = partId;
                    progress.equippedParts = list.ToArray();
                    return;
                }
            }

            list.Add(new PlayerProgress.EquippedPart { category = cat, partId = partId });
            progress.equippedParts = list.ToArray();
        }

        // ---------- NEW badge tracking ----------

        public static bool HasSeenPart(this PlayerProgress progress, string partId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(partId) || progress.seenPartIds == null)
            {
                return false;
            }

            for (int i = 0; i < progress.seenPartIds.Length; i++)
            {
                if (progress.seenPartIds[i] == partId)
                {
                    return true;
                }
            }
            return false;
        }

        public static void MarkPartSeen(this PlayerProgress progress, string partId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(partId))
            {
                return;
            }
            if (progress.HasSeenPart(partId)) return;

            var old = progress.seenPartIds ?? Array.Empty<string>();
            var next = new string[old.Length + 1];
            Array.Copy(old, next, old.Length);
            next[next.Length - 1] = partId;
            progress.seenPartIds = next;
        }

        // ---------- Contract completion tracking ----------

        public static bool HasCompletedContract(this PlayerProgress progress, string contractId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(contractId) || progress.completedContractIds == null)
            {
                return false;
            }
            for (int i = 0; i < progress.completedContractIds.Length; i++)
            {
                if (progress.completedContractIds[i] == contractId) return true;
            }
            return false;
        }

        public static void MarkContractCompleted(this PlayerProgress progress, string contractId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(contractId)) return;
            if (progress.HasCompletedContract(contractId)) return;

            var old = progress.completedContractIds ?? Array.Empty<string>();
            var next = new string[old.Length + 1];
            Array.Copy(old, next, old.Length);
            next[next.Length - 1] = contractId;
            progress.completedContractIds = next;
        }

        // ---------- Ammo selection ----------

        public static string GetSelectedAmmoId(this PlayerProgress progress)
        {
            return progress?.selectedAmmoId ?? string.Empty;
        }

        public static void SetSelectedAmmoId(this PlayerProgress progress, string ammoId)
        {
            if (progress == null) return;
            progress.selectedAmmoId = ammoId ?? string.Empty;
        }
    }
}
