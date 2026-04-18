using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BarnSwarmSniper.Level
{
    public class LightingModeController : MonoBehaviour
    {
        public enum LightingMode { Daylight, NightVision, ThermalWhiteHot, ThermalGreen }

        [SerializeField] private Volume _globalPostProcessVolume; // Assign your global post-processing volume here
        [SerializeField] private Color _daylightAmbientColor = Color.white;
        [SerializeField] private Color _nightVisionAmbientColor = new Color(0.1f, 0.2f, 0.1f);
        [SerializeField] private Color _thermalAmbientColor = Color.black;

        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;
        private FilmGrain _filmGrain;

        void Start()
        {
            if (_globalPostProcessVolume == null)
            {
                Debug.LogWarning("LightingModeController: Global Post Process Volume not assigned. Visual effects for lighting modes will not work.");
                return;
            }

            // Try to get components from the volume profile
            _globalPostProcessVolume.profile.TryGet(out _vignette);
            _globalPostProcessVolume.profile.TryGet(out _colorAdjustments);
            _globalPostProcessVolume.profile.TryGet(out _filmGrain);

            SetLightingMode(LightingMode.Daylight); // Default to daylight
        }

        public void SetLightingMode(LightingMode mode)
        {
            // Reset all effects first
            ResetPostProcessingEffects();

            switch (mode)
            {
                case LightingMode.Daylight:
                    RenderSettings.ambientLight = _daylightAmbientColor;
                    // No specific post-processing for daylight, or subtle adjustments
                    break;
                case LightingMode.NightVision:
                    RenderSettings.ambientLight = _nightVisionAmbientColor;
                    if (_colorAdjustments != null)
                    {
                        _colorAdjustments.saturation.value = -100f; // Desaturate
                        _colorAdjustments.postExposure.value = -2f; // Darken
                        _colorAdjustments.colorFilter.value = new Color(0.1f, 1f, 0.1f); // Green tint
                    }
                    if (_filmGrain != null)
                    {
                        _filmGrain.active = true;
                        _filmGrain.intensity.value = 0.5f;
                    }
                    if (_vignette != null)
                    {
                        _vignette.active = true;
                        _vignette.intensity.value = 0.4f;
                    }
                    break;
                case LightingMode.ThermalWhiteHot:
                    RenderSettings.ambientLight = _thermalAmbientColor;
                    if (_colorAdjustments != null)
                    {
                        _colorAdjustments.saturation.value = -100f;
                        _colorAdjustments.postExposure.value = 0f;
                        _colorAdjustments.colorFilter.value = Color.white; // Base for white hot
                        // A custom shader would be needed for true thermal vision color mapping
                    }
                    if (_vignette != null)
                    {
                        _vignette.active = true;
                        _vignette.intensity.value = 0.4f;
                    }
                    break;
                case LightingMode.ThermalGreen:
                    RenderSettings.ambientLight = _thermalAmbientColor;
                    if (_colorAdjustments != null)
                    {
                        _colorAdjustments.saturation.value = -100f;
                        _colorAdjustments.postExposure.value = 0f;
                        _colorAdjustments.colorFilter.value = new Color(0.1f, 0.5f, 0.1f); // Green tint for thermal
                        // A custom shader would be needed for true thermal vision color mapping
                    }
                    if (_vignette != null)
                    {
                        _vignette.active = true;
                        _vignette.intensity.value = 0.4f;
                    }
                    break;
            }
        }

        public void SetRandomLightingMode()
        {
            LightingMode[] modes = (LightingMode[])System.Enum.GetValues(typeof(LightingMode));
            LightingMode randomMode = modes[Random.Range(0, modes.Length)];
            SetLightingMode(randomMode);
        }

        private void ResetPostProcessingEffects()
        {
            if (_vignette != null) _vignette.active = false;
            if (_filmGrain != null) _filmGrain.active = false;
            if (_colorAdjustments != null)
            {
                _colorAdjustments.saturation.value = 0f;
                _colorAdjustments.postExposure.value = 0f;
                _colorAdjustments.colorFilter.value = Color.white;
            }
        }
    }
}
