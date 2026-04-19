using UnityEngine;
using BarnSwarmSniper.Data;
using BarnSwarmSniper.Weapon;

namespace BarnSwarmSniper.Input
{
    public class HybridInputController : MonoBehaviour
    {
        [SerializeField] private GyroInputHandler _gyroInputHandler;
        [SerializeField] private TouchInputHandler _touchInputHandler;
        [SerializeField] private InputSmoothingPipeline _smoothingPipeline;
        [SerializeField] private SettingsData _settingsData;
        [SerializeField] private WeaponController _weaponController; // For aim assist

        [Header("Dead Zone Settings")]
        [SerializeField] private float _gyroDeadZone = 0.5f; // Degrees per second threshold

        public Vector2 LookInput { get; private set; }
        public bool FireInput { get; private set; }

        void Update()
        {
            Vector2 rawGyroDelta = _gyroInputHandler.GyroDelta;
            Vector2 rawTouchDelta = _touchInputHandler.TouchDelta;

            // Apply dead zone to gyro input
            if (rawGyroDelta.magnitude < _gyroDeadZone)
            {
                rawGyroDelta = Vector2.zero;
            }

            Vector2 smoothedGyroDelta = _smoothingPipeline.GetSmoothedGyroDelta(rawGyroDelta);
            Vector2 smoothedTouchDelta = _smoothingPipeline.GetSmoothedTouchDelta(rawTouchDelta);

            // Combine inputs
            Vector2 combinedInput = smoothedGyroDelta + smoothedTouchDelta;

            // Apply aim assist (placeholder for now, actual logic will be in WeaponController)
            // The HybridInputController will provide the base input, and WeaponController will modify it.
            // For now, we'll just pass the combined input.
            LookInput = combinedInput;

            FireInput = _touchInputHandler.IsFiring;

            // Pass input to WeaponController for firing and aim assist application
            if (_weaponController != null)
            {
                _weaponController.ReceiveInput(LookInput, FireInput);
            }
        }

        public void ResetInput()
        {
            _smoothingPipeline.ResetSmoothing();
            _gyroInputHandler.RecenterGyro();
            LookInput = Vector2.zero;
            FireInput = false;
        }
    }
}
