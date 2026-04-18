using UnityEngine;
using BarnSwarmSniper.Weapon;

namespace BarnSwarmSniper.Camera
{
    public class ZoomController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera _mainCamera;
        [SerializeField] private OpticsTierConfig _opticsConfig; // Reference to optics configuration

        private float _targetFOV;
        private float _currentFOV;
        private float _zoomSpeed = 5f; // Speed of zoom interpolation

        void Start()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GetComponent<UnityEngine.Camera>();
                if (_mainCamera == null)
                {
                    Debug.LogError("ZoomController: Main Camera not assigned and not found on GameObject.");
                    enabled = false;
                    return;
                }
            }

            if (_opticsConfig == null)
            {
                Debug.LogError("ZoomController: OpticsTierConfig not assigned.");
                enabled = false;
                return;
            }

            _currentFOV = _mainCamera.fieldOfView;
            _targetFOV = _currentFOV;
        }

        void Update()
        {
            // Interpolate FOV towards target FOV
            _mainCamera.fieldOfView = Mathf.Lerp(_mainCamera.fieldOfView, _targetFOV, Time.deltaTime * _zoomSpeed);
        }

        public void SetZoomLevel(int tierIndex)
        {
            if (_opticsConfig != null && tierIndex >= 0 && tierIndex < _opticsConfig.OpticsTiers.Length)
            {
                _targetFOV = _opticsConfig.OpticsTiers[tierIndex].FieldOfView;
            }
            else
            {
                Debug.LogWarning($"ZoomController: Invalid optics tier index {tierIndex} or OpticsTierConfig not set.");
            }
        }

        public float CurrentZoomFactor()
        {
            // Assuming default FOV is 60 for 1x zoom
            return 60f / _mainCamera.fieldOfView;
        }
    }
}
