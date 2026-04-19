using System;
using BarnSwarmSniper.Weapon;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BarnSwarmSniper.UI
{
    /// <summary>
    /// Controls the scope overlay HUD and the active URP post-processing Volume
    /// per scope mode. Emits a mode-change event that other systems (rat
    /// eye-shine, weapon controller) can subscribe to.
    /// </summary>
    public class ScopeOverlayController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image _vignetteImage;
        [SerializeField] private Image _reticleImage;
        [SerializeField] private TextMeshProUGUI _zoomText;
        [SerializeField] private TextMeshProUGUI _modeText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _killCountText;
        [SerializeField] private GameObject _recIcon;

        [Header("HUD Accent (tinted to current scope mode)")]
        [SerializeField] private Graphic[] _hudAccentTargets;

        [Header("URP Volume")]
        [SerializeField] private Volume _globalScopeVolume;
        [SerializeField] private ScopeVolumeProfileSet _profileSet;
        [SerializeField] private float _volumeWeightOn = 1f;

        public enum ScopeMode { Daylight, NightVision, ThermalWhiteHot, ThermalGreen }
        [Header("Scope Modes")]
        [SerializeField] private ScopeMode _defaultScopeMode = ScopeMode.Daylight;

        private ScopeMode _currentScopeMode = ScopeMode.Daylight;
        public ScopeMode CurrentScopeMode => _currentScopeMode;

        /// <summary>Fires when the scope mode changes (subscribed by RatEyeShine etc.).</summary>
        public static event Action<ScopeMode, ScopeVolumeProfileSet.Entry> OnScopeModeChanged;

        private void Start()
        {
            SwitchScopeMode(_defaultScopeMode);
        }

        public void UpdateZoom(float zoomFactor)
        {
            if (_zoomText != null)
            {
                _zoomText.text = $"{zoomFactor:F1}x";
            }
        }

        public void UpdateTimer(float timeRemaining)
        {
            if (_timerText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                int seconds = Mathf.FloorToInt(timeRemaining % 60);
                _timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        public void UpdateKillCount(int count)
        {
            if (_killCountText != null)
            {
                _killCountText.text = $"KILLS: {count}";
            }
        }

        public void SetRecIconActive(bool active)
        {
            if (_recIcon != null)
            {
                _recIcon.SetActive(active);
            }
        }

        public void SwitchScopeMode(ScopeMode newMode)
        {
            _currentScopeMode = newMode;
            UpdateScopeModeDisplay();

            ScopeVolumeProfileSet.Entry entry = null;
            if (_profileSet != null) _profileSet.TryGet(newMode, out entry);

            ApplyVolume(entry);
            ApplyOverlayVisuals(entry, newMode);

            OnScopeModeChanged?.Invoke(newMode, entry);
        }

        /// <summary>Cycles through a provided list of supported modes (wraps). Returns the new mode.</summary>
        public ScopeMode CycleMode(System.Collections.Generic.IList<ScopeMode> allowedModes)
        {
            if (allowedModes == null || allowedModes.Count == 0)
            {
                return _currentScopeMode;
            }

            int idx = allowedModes.IndexOf(_currentScopeMode);
            idx = (idx + 1) % allowedModes.Count;
            SwitchScopeMode(allowedModes[idx]);
            return _currentScopeMode;
        }

        /// <summary>Clamps the current scope mode to something the given optics tier supports.</summary>
        public void EnforceSupportedMode(OpticsTierConfig.OpticsTier tier)
        {
            if (tier == null) return;
            if (!tier.SupportsMode(_currentScopeMode))
            {
                SwitchScopeMode(tier.defaultScopeMode);
            }
        }

        private void UpdateScopeModeDisplay()
        {
            if (_modeText != null)
            {
                _modeText.text = _currentScopeMode.ToString().ToUpper();
            }
        }

        private void ApplyVolume(ScopeVolumeProfileSet.Entry entry)
        {
            if (_globalScopeVolume == null) return;
            if (entry == null || entry.profile == null)
            {
                // No profile for this mode -> disable override entirely.
                _globalScopeVolume.weight = 0f;
                return;
            }

            _globalScopeVolume.profile = entry.profile;
            _globalScopeVolume.weight = Mathf.Clamp01(_volumeWeightOn);
        }

        private void ApplyOverlayVisuals(ScopeVolumeProfileSet.Entry entry, ScopeMode mode)
        {
            Color accent = Color.white;
            if (entry != null)
            {
                accent = entry.hudAccentColor;
                if (_reticleImage != null)
                {
                    if (entry.reticleSprite != null) _reticleImage.sprite = entry.reticleSprite;
                    _reticleImage.color = entry.reticleColor;
                }
            }
            else
            {
                // Fallback per-mode tints so it still feels different without assets wired.
                accent = mode switch
                {
                    ScopeMode.NightVision       => new Color(0.3f, 1f, 0.3f),
                    ScopeMode.ThermalWhiteHot   => new Color(1f, 1f, 1f),
                    ScopeMode.ThermalGreen      => new Color(0.4f, 1f, 0.6f),
                    _                            => Color.white
                };
                if (_reticleImage != null) _reticleImage.color = accent;
            }

            if (_modeText != null) _modeText.color = accent;

            if (_hudAccentTargets != null)
            {
                for (int i = 0; i < _hudAccentTargets.Length; i++)
                {
                    if (_hudAccentTargets[i] != null) _hudAccentTargets[i].color = accent;
                }
            }
        }
    }
}
