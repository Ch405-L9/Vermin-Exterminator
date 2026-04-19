using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarnSwarmSniper.Weapon
{
    [CreateAssetMenu(fileName = "OpticsTierConfig", menuName = "BarnSwarmSniper/Optics Tier Config", order = 3)]
    public class OpticsTierConfig : ScriptableObject
    {
        [Serializable]
        public class OpticsTier
        {
            [Header("Identity")]
            public string tierName;
            [TextArea(1, 3)] public string description;

            [Header("Zoom / FOV")]
            public float zoomLevel;
            public float FieldOfView;            // Primary FOV used at this tier
            public float minFieldOfView = 0f;    // Optional zoom range (0 = disabled)
            public float maxFieldOfView = 0f;    // Optional zoom range (0 = disabled)

            [Header("Handling")]
            [Tooltip("Per-tier sway multiplier (lower = steadier optic).")]
            public float swayMultiplier = 1f;
            [Tooltip("Per-tier aim-assist cone multiplier (lower = tighter magnetism).")]
            public float aimAssistConeMultiplier = 1f;

            [Header("Supported Scope Modes")]
            public BarnSwarmSniper.UI.ScopeOverlayController.ScopeMode defaultScopeMode;
            public List<BarnSwarmSniper.UI.ScopeOverlayController.ScopeMode> supportedScopeModes =
                new List<BarnSwarmSniper.UI.ScopeOverlayController.ScopeMode>();

            public bool SupportsMode(BarnSwarmSniper.UI.ScopeOverlayController.ScopeMode mode)
            {
                if (supportedScopeModes == null || supportedScopeModes.Count == 0)
                {
                    // Backward compat: if nothing set, only default is supported.
                    return mode == defaultScopeMode;
                }
                return supportedScopeModes.Contains(mode);
            }
        }

        public OpticsTier[] OpticsTiers;

        public bool TryGetTier(int index, out OpticsTier tier)
        {
            if (OpticsTiers != null && index >= 0 && index < OpticsTiers.Length)
            {
                tier = OpticsTiers[index];
                return tier != null;
            }
            tier = null;
            return false;
        }
    }
}
