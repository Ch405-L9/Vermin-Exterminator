using BarnSwarmSniper.UI;
using BarnSwarmSniper.Weapon;
using UnityEngine;

namespace BarnSwarmSniper.AI
{
    /// <summary>
    /// Drives the emissive "eye shine" on a rat's eye material based on the
    /// current scope mode. Subscribe in OnEnable/OnDisable so pooled rats
    /// update correctly when recycled.
    ///
    /// Attach to the rat prefab. Assign the eye Renderer and the material
    /// property name (defaults to URP Lit's _EmissionColor).
    /// </summary>
    public class RatEyeShine : MonoBehaviour
    {
        [SerializeField] private Renderer _eyeRenderer;
        [SerializeField] private string _emissionProperty = "_EmissionColor";
        [SerializeField] private Color _baseColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        [SerializeField] private float _baseIntensity = 0.2f;

        private MaterialPropertyBlock _mpb;
        private int _propId;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _propId = Shader.PropertyToID(_emissionProperty);
            if (_eyeRenderer == null) _eyeRenderer = GetComponentInChildren<Renderer>();
            ApplyColor(_baseColor, _baseIntensity);
        }

        private void OnEnable()
        {
            ScopeOverlayController.OnScopeModeChanged += HandleScopeModeChanged;
            // Apply current mode's visuals when (re)activated from pool.
            var controller = Object.FindFirstObjectByType<ScopeOverlayController>(FindObjectsInactive.Include);
            if (controller != null)
            {
                ForceApplyForMode(controller.CurrentScopeMode);
            }
        }

        private void OnDisable()
        {
            ScopeOverlayController.OnScopeModeChanged -= HandleScopeModeChanged;
        }

        private void HandleScopeModeChanged(ScopeOverlayController.ScopeMode mode, ScopeVolumeProfileSet.Entry entry)
        {
            if (entry != null)
            {
                ApplyColor(entry.ratEyeShineColor, entry.ratEyeShineIntensity);
            }
            else
            {
                ForceApplyForMode(mode);
            }
        }

        private void ForceApplyForMode(ScopeOverlayController.ScopeMode mode)
        {
            switch (mode)
            {
                case ScopeOverlayController.ScopeMode.NightVision:
                    ApplyColor(new Color(0.2f, 1f, 0.2f), 3.0f);
                    break;
                case ScopeOverlayController.ScopeMode.ThermalWhiteHot:
                    ApplyColor(Color.white, 4.0f);
                    break;
                case ScopeOverlayController.ScopeMode.ThermalGreen:
                    ApplyColor(new Color(0.4f, 1f, 0.5f), 3.5f);
                    break;
                default:
                    ApplyColor(_baseColor, _baseIntensity);
                    break;
            }
        }

        private void ApplyColor(Color color, float intensity)
        {
            if (_eyeRenderer == null || _mpb == null) return;
            _eyeRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_propId, color * Mathf.Max(0f, intensity));
            _eyeRenderer.SetPropertyBlock(_mpb);
        }
    }
}
