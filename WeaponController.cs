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

        void Start()
        {
            _baseFireRate = _fireRate;
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) Debug.LogWarning("WeaponController: No AudioSource found on GameObject.");

            // Initialize zoom to default tier
            if (_opticsConfig != null && _opticsConfig.OpticsTiers.Length > 0)
            {
                _zoomController.SetZoomLevel(_currentOpticsTierIndex);
            }
        }

        public void ApplyModifiers(WeaponStatModifiers modifiers)
        {
            if (modifiers == null)
            {
                _fireRate = _baseFireRate;
                _maxAllowedOpticsTierIndex = int.MaxValue;
                return;
            }

            _fireRate = Mathf.Max(0.05f, _baseFireRate * modifiers.fireRateMultiplier);

            if (_recoilSystem != null)
            {
                _recoilSystem.ApplyMultipliers(modifiers.recoilAmountMultiplier, modifiers.recoilRecoveryMultiplier);
            }

            EnsureRuntimeSettings();
            if (_runtimeSettings != null)
            {
                _runtimeSettings.AimAssistFrictionStrength *= modifiers.aimAssistFrictionStrengthMultiplier;
                _runtimeSettings.AimAssistFrictionRadius *= modifiers.aimAssistFrictionRadiusMultiplier;
            }

            if (_opticsConfig != null && _opticsConfig.OpticsTiers != null)
            {
                _maxAllowedOpticsTierIndex = Mathf.Clamp(
                    (_opticsConfig.OpticsTiers.Length - 1) + modifiers.maxOpticsTierDelta,
                    0,
                    _opticsConfig.OpticsTiers.Length - 1);
            }

            _currentOpticsTierIndex = Mathf.Clamp(_currentOpticsTierIndex, 0, _maxAllowedOpticsTierIndex);
            if (_zoomController != null)
            {
                _zoomController.SetZoomLevel(_currentOpticsTierIndex);
            }
        }

        public void ReceiveInput(Vector2 lookInput, bool fireInput)
        {
            // Aim assist application (before raycast)
            Vector3 aimDirection = GetAimDirection(lookInput);

            if (fireInput && Time.time >= _nextFireTime)
            {
                Fire(aimDirection);
                _nextFireTime = Time.time + 1f / _fireRate;
            }
        }

        private Vector3 GetAimDirection(Vector2 lookInput)
        {
            // This is where aim assist logic would be applied to the raw lookInput
            // For now, it just returns the camera's forward direction, potentially modified by lookInput
            Vector3 baseDirection = _cameraController.transform.forward;
            EnsureRuntimeSettings();
            float frictionRadius = _runtimeSettings != null ? _runtimeSettings.AimAssistFrictionRadius : (_settingsData != null ? _settingsData.AimAssistFrictionRadius : 0.1f);
            float frictionStrength = _runtimeSettings != null ? _runtimeSettings.AimAssistFrictionStrength : (_settingsData != null ? _settingsData.AimAssistFrictionStrength : 0.5f);

            // Simple aim assist: check for nearby rats and nudge aim
            RaycastHit hit;
            if (Physics.SphereCast(_cameraController.transform.position, frictionRadius, baseDirection, out hit, 100f, _ratLayer))
            {
                // If a rat is within the friction radius, gently pull the aim towards it
                Vector3 targetDirection = (hit.collider.transform.position - _cameraController.transform.position).normalized;
                baseDirection = Vector3.Lerp(baseDirection, targetDirection, frictionStrength);
            }

            return baseDirection;
        }

        private void Fire(Vector3 aimDirection)
        {
            if (_audioSource != null && _shootSound != null) _audioSource.PlayOneShot(_shootSound);

            _recoilSystem.AddRecoil(new Vector3(-0.5f, Random.Range(-0.2f, 0.2f), 0f)); // Apply recoil

            RaycastHit hit;
            if (Physics.Raycast(_cameraController.transform.position, aimDirection, out hit, Mathf.Infinity, _ratLayer))
            {
                Debug.Log($"Hit: {hit.collider.name}");
                RatFSM rat = hit.collider.GetComponent<RatFSM>();
                if (rat != null)
                {
                    rat.OnShot();
                    _ratAIManager.DespawnRat(rat); // RatAIManager handles pooling and score notification
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
            if (_opticsConfig != null && _opticsConfig.OpticsTiers.Length > 0)
            {
                int tierCount = Mathf.Min(_opticsConfig.OpticsTiers.Length, _maxAllowedOpticsTierIndex + 1);
                tierCount = Mathf.Max(1, tierCount);
                _currentOpticsTierIndex = (_currentOpticsTierIndex + 1) % tierCount;
                _zoomController.SetZoomLevel(_currentOpticsTierIndex);
                // Also update ScopeOverlayController with new mode if applicable
            }
        }

        public int GetCurrentOpticsTierIndex()
        {
            return _currentOpticsTierIndex;
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
    }
}
