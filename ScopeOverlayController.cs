using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BarnSwarmSniper.UI
{
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

        [Header("Scope Modes")]
        public enum ScopeMode { Daylight, NightVision, ThermalWhiteHot, ThermalGreen }
        private ScopeMode _currentScopeMode = ScopeMode.Daylight;

        void Start()
        {
            UpdateScopeModeDisplay();
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
            ApplyScopeModeVisuals(newMode);
        }

        private void UpdateScopeModeDisplay()
        {
            if (_modeText != null)
            {
                _modeText.text = _currentScopeMode.ToString().ToUpper();
            }
        }

        private void ApplyScopeModeVisuals(ScopeMode mode)
        {
            // TODO: Implement URP Render Features or Post-Processing for visual effects
            // This will involve setting parameters on a global volume or custom renderer features.
            switch (mode)
            {
                case ScopeMode.Daylight:
                    // Reset any post-processing effects
                    break;
                case ScopeMode.NightVision:
                    // Apply green tint, noise, bloom effects via post-processing
                    break;
                case ScopeMode.ThermalWhiteHot:
                    // Apply white-hot thermal color grading via post-processing
                    break;
                case ScopeMode.ThermalGreen:
                    // Apply green thermal color grading via post-processing
                    break;
            }
        }
    }
}
