using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BarnSwarmSniper.Weapon
{
    /// <summary>
    /// Maps each ScopeMode to a URP VolumeProfile plus visual accents used by
    /// the ScopeOverlayController (reticle sprite/color, HUD accent color,
    /// rat eye-shine emissive intensity).
    ///
    /// Keep all of this in a single ScriptableObject so artists can iterate
    /// without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "ScopeVolumeProfileSet", menuName = "BarnSwarmSniper/Scope Volume Profile Set", order = 6)]
    public class ScopeVolumeProfileSet : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public BarnSwarmSniper.UI.ScopeOverlayController.ScopeMode mode;
            public VolumeProfile profile;

            [Header("Overlay / HUD accents")]
            public Sprite reticleSprite;
            public Color reticleColor = Color.white;
            public Color hudAccentColor = Color.white;

            [Header("Rat Eye-Shine")]
            [Tooltip("Emission intensity multiplier applied to rat eyes (1.0 = neutral).")]
            public float ratEyeShineIntensity = 1f;
            public Color ratEyeShineColor = new Color(0.1f, 1f, 0.1f, 1f);
        }

        public List<Entry> entries = new List<Entry>();

        public bool TryGet(BarnSwarmSniper.UI.ScopeOverlayController.ScopeMode mode, out Entry entry)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e != null && e.mode == mode)
                {
                    entry = e;
                    return true;
                }
            }
            entry = null;
            return false;
        }
    }
}
