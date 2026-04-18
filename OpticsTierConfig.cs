using UnityEngine;

namespace BarnSwarmSniper.Weapon
{
    [CreateAssetMenu(fileName = "OpticsTierConfig", menuName = "BarnSwarmSniper/Optics Tier Config", order = 3)]
    public class OpticsTierConfig : ScriptableObject
    {
        [System.Serializable]
        public class OpticsTier
        {
            public string tierName;
            public float zoomLevel;
            public float FieldOfView; // Corresponding FOV for this zoom level
            public ScopeOverlayController.ScopeMode defaultScopeMode; // Default mode for this tier
        }

        public OpticsTier[] OpticsTiers;
    }
}
