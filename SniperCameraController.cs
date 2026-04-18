using UnityEngine;
using BarnSwarmSniper.Input;
using BarnSwarmSniper.Weapon;

namespace BarnSwarmSniper.Camera
{
    public class SniperCameraController : MonoBehaviour
    {
        [SerializeField] private HybridInputController _inputController;
        [SerializeField] private Transform _cameraRoot; // The transform that holds the MainCamera
        [SerializeField] private RecoilSystem _recoilSystem; // Reference to the recoil system

        private float _currentPitch = 0f;
        private float _currentYaw = 0f;

        void LateUpdate()
        {
            if (_inputController == null || _cameraRoot == null)
            {
                Debug.LogWarning("SniperCameraController: Missing input controller or camera root reference.");
                return;
            }

            // Get look input from the HybridInputController
            Vector2 lookInput = _inputController.LookInput;

            // Apply input to pitch and yaw
            _currentYaw += lookInput.x * Time.deltaTime; // Yaw (left/right) affects Y-axis rotation
            _currentPitch -= lookInput.y * Time.deltaTime; // Pitch (up/down) affects X-axis rotation

            // Clamp pitch to prevent camera from flipping over
            _currentPitch = Mathf.Clamp(_currentPitch, -80f, 80f);

            // Apply rotation to the camera root
            _cameraRoot.localRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);

            // Apply recoil to the camera root
            if (_recoilSystem != null)
            {
                _cameraRoot.localRotation *= _recoilSystem.CurrentRecoilRotation;
            }
        }

        public void ApplyRecoil(Vector3 recoilForce)
        {
            if (_recoilSystem != null)
            {
                _recoilSystem.AddRecoil(recoilForce);
            }
        }
    }
}
