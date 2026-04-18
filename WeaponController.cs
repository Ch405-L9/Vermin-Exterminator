using UnityEngine;
using BarnSwarmSniper.Input;
using BarnSwarmSniper.Camera;
using BarnSwarmSniper.AI;
using BarnSwarmSniper.Scoring;
using BarnSwarmSniper.UI;
using BarnSwarmSniper.Data;
using BarnSwarmSniper.Game;

namespace BarnSwarmSniper.Weapon
{
    public class WeaponController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SniperCameraController _cameraController;
        [SerializeField] private ZoomController _zoomController;
        [SerializeField] private OpticsTierConfig _opticsConfig;
        [SerializeField] private RecoilSystem _recoilSystem;
        [SerializeField] private RatAIManager _ratAIManager;
        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private EffectsObjectPool _effectsObjectPool;
        [SerializeField] private SettingsData _settingsData;
        [SerializeField] private ScopeOverlayController _scopeOverlay;

        [Header("Weapon Settings")]
        [SerializeField] private float _fireRate = 0.5f; // Shots per second
        [SerializeField] private LayerMask _ratLayer; // Layer containing rats
        [SerializeField] private AudioClip _shootSound;
        [SerializeField] private AudioSource _audioSource;

        private float _nextFireTime = 0f;
        private int _currentOpticsTierIndex = 0;
        private int _maxAllowedOpticsTierIndex = int.MaxValue;

        private float _baseFireRate;
        private RuntimeSettings _runtimeSettings;
        private WeaponStatModifiers _activeModifiers;

        // Base (unscaled) aim-assist values captured from runtime settings so we
        // can safely re-apply the per-tier multipliers when tier changes.
        private float _baseAimAssistRadius;
        private float _baseAimAssistStrength;
        private bool _aimAssistBaseCaptured;

        void Start()
        {
            _baseFireRate = _fireRate;
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) Debug.LogWarning("WeaponController: No AudioSource found on GameObject.");

            if (_opticsConfig != null && _opticsConfig.OpticsTiers != null && _opticsConfig.OpticsTiers.Length > 0)
            {
                ApplyCurrentTier();
            }
        }

        public void ApplyModifiers(WeaponStatModifiers modifiers)
        {
            _activeModifiers = modifiers;

            if (modifiers == null)
            {
                _fireRate = _baseFireRate;
                _maxAllowedOpticsTierIndex = int.MaxValue;
                ApplyCurrentTier();
                return;
            }

            _fireRate = Mathf.Max(0.05f, _baseFireRate * modifiers.fireRateMultiplier);

            if (_recoilSystem != null)
            {
                _recoilSystem.ApplyMultipliers(modifiers.recoilAmountMultiplier, modifiers.recoilRecoveryMultiplier);
            }

            EnsureRuntimeSettings();
            CaptureAimAssistBase();
            if (_runtimeSettings != null)
            {
                _runtimeSettings.AimAssistFrictionStrength = _baseAimAssistStrength * modifiers.aimAssistFrictionStrengthMultiplier;
                _runtimeSettings.AimAssistFrictionRadius = _baseAimAssistRadius * modifiers.aimAssistFrictionRadiusMultiplier;
            }

            if (_opticsConfig != null && _opticsConfig.OpticsTiers != null)
            {
                _maxAllowedOpticsTierIndex = Mathf.Clamp(
                    (_opticsConfig.OpticsTiers.Length - 1) + modifiers.maxOpticsTierDelta,
                    0,
                    _opticsConfig.OpticsTiers.Length - 1);
            }

            _currentOpticsTierIndex = Mathf.Clamp(_currentOpticsTierIndex, 0, _maxAllowedOpticsTierIndex);
            ApplyCurrentTier();
        }

        public void ReceiveInput(Vector2 lookInput, bool fireInput)
        {
            Vector3 aimDirection = GetAimDirection(lookInput);

            if (fireInput && Time.time >= _nextFireTime)
            {
                Fire(aimDirection);
                _nextFireTime = Time.time + 1f / _fireRate;
            }
        }

        private Vector3 GetAimDirection(Vector2 lookInput)
        {
            Vector3 baseDirection = _cameraController.transform.forward;
            EnsureRuntimeSettings();
            float frictionRadius = _runtimeSettings != null ? _runtimeSettings.AimAssistFrictionRadius : (_settingsData != null ? _settingsData.AimAssistFrictionRadius : 0.1f);
            float frictionStrength = _runtimeSettings != null ? _runtimeSettings.AimAssistFrictionStrength : (_settingsData != null ? _settingsData.AimAssistFrictionStrength : 0.5f);

            RaycastHit hit;
            if (Physics.SphereCast(_cameraController.transform.position, frictionRadius, baseDirection, out hit, 100f, _ratLayer))
            {
                Vector3 targetDirection = (hit.collider.transform.position - _cameraController.transform.position).normalized;
                baseDirection = Vector3.Lerp(baseDirection, targetDirection, frictionStrength);
            }

            return baseDirection;
        }

        private void Fire(Vector3 aimDirection)
        {
            if (_audioSource != null && _shootSound != null) _audioSource.PlayOneShot(_shootSound);

            _recoilSystem.AddRecoil(new Vector3(-0.5f, Random.Range(-0.2f, 0.2f), 0f));

            RaycastHit hit;
            if (Physics.Raycast(_cameraController.transform.position, aimDirection, out hit, Mathf.Infinity, _ratLayer))
            {
                Debug.Log($"Hit: {hit.collider.name}");
                RatFSM rat = hit.collider.GetComponent<RatFSM>();
                if (rat != null)
                {
                    rat.OnShot();
                    _ratAIManager.DespawnRat(rat);
                    _effectsObjectPool.GetHitMarker(hit.point, Quaternion.LookRotation(hit.normal));
                    _effectsObjectPool.GetImpactDust(hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
            else
            {
                _scoreManager.NotifyMiss();
            }
        }

        public void CycleOpticsTier()
        {
            if (_opticsConfig == null || _opticsConfig.OpticsTiers == null || _opticsConfig.OpticsTiers.Length == 0) return;

            int tierCount = Mathf.Min(_opticsConfig.OpticsTiers.Length, _maxAllowedOpticsTierIndex + 1);
            tierCount = Mathf.Max(1, tierCount);
            _currentOpticsTierIndex = (_currentOpticsTierIndex + 1) % tierCount;
            ApplyCurrentTier();
        }

        /// <summary>Cycles the current scope mode through the modes supported by the active optics tier.</summary>
        public void CycleScopeMode()
        {
            if (_scopeOverlay == null || _opticsConfig == null) return;
            if (!_opticsConfig.TryGetTier(_currentOpticsTierIndex, out var tier) || tier == null) return;
            if (tier.supportedScopeModes == null || tier.supportedScopeModes.Count == 0)
            {
                _scopeOverlay.SwitchScopeMode(tier.defaultScopeMode);
                return;
            }
            _scopeOverlay.CycleMode(tier.supportedScopeModes);
        }

        public int GetCurrentOpticsTierIndex() => _currentOpticsTierIndex;

        private void ApplyCurrentTier()
        {
            if (_opticsConfig == null || _opticsConfig.OpticsTiers == null || _opticsConfig.OpticsTiers.Length == 0) return;

            _currentOpticsTierIndex = Mathf.Clamp(_currentOpticsTierIndex, 0, _opticsConfig.OpticsTiers.Length - 1);

            if (_zoomController != null)
            {
                _zoomController.SetZoomLevel(_currentOpticsTierIndex);
            }

            if (!_opticsConfig.TryGetTier(_currentOpticsTierIndex, out var tier) || tier == null) return;

            // Per-tier sway / aim-assist scaling (applied on top of equipped-part modifiers).
            EnsureRuntimeSettings();
            CaptureAimAssistBase();
            if (_runtimeSettings != null)
            {
                float partRadiusMult = _activeModifiers != null ? _activeModifiers.aimAssistFrictionRadiusMultiplier : 1f;
                float partStrengthMult = _activeModifiers != null ? _activeModifiers.aimAssistFrictionStrengthMultiplier : 1f;
                _runtimeSettings.AimAssistFrictionRadius = _baseAimAssistRadius * partRadiusMult * Mathf.Max(0.0001f, tier.aimAssistConeMultiplier);
                _runtimeSettings.AimAssistFrictionStrength = _baseAimAssistStrength * partStrengthMult;
            }

            // Scope overlay clamp to supported modes
            if (_scopeOverlay != null)
            {
                _scopeOverlay.EnforceSupportedMode(tier);
            }
        }

        private void EnsureRuntimeSettings()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentRuntimeSettings != null)
            {
                _runtimeSettings = GameManager.Instance.CurrentRuntimeSettings;
                return;
            }

            if (_runtimeSettings == null && _settingsData != null)
            {
                _runtimeSettings = RuntimeSettings.FromSettingsData(_settingsData);
            }
        }

        private void CaptureAimAssistBase()
        {
            if (_aimAssistBaseCaptured || _runtimeSettings == null) return;
            _baseAimAssistRadius = _runtimeSettings.AimAssistFrictionRadius;
            _baseAimAssistStrength = _runtimeSettings.AimAssistFrictionStrength;
            _aimAssistBaseCaptured = true;
        }
    }
}
