using UnityEngine;
using System;

namespace BarnSwarmSniper.Data
{
    public class LevelSeedGenerator : MonoBehaviour
    {
        public int GenerateSeed(int playerLevel, DateTime currentDate, string deviceID)
        {
            // Combine player level, date, and device ID into a string
            string seedString = $"{playerLevel}-{currentDate.Year}-{currentDate.Month}-{currentDate.Day}-{deviceID}";

            // Use the hash code of the string as the seed
            // This provides a deterministic seed based on the inputs
            return seedString.GetHashCode();
        }
    }
}
