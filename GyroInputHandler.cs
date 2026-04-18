using UnityEngine;
using BarnSwarmSniper.Data;
using BarnSwarmSniper.Game;

namespace BarnSwarmSniper.Input
{
    public class GyroInputHandler : MonoBehaviour
    {
        [SerializeField] private SettingsData _settingsData;
        [SerializeField] private float _maxAngularVelocity = 100f; // Degrees per second

        private Quaternion _initialRotation;
        private Quaternion _gyroCorrection;
        private bool _gyroEnabled;

        public Vector2 GyroDelta { get; private set; }

        void Awake()
        {
            _gyroEnabled = EnableGyro();
        }

        private bool EnableGyro()
        {
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                _initialRotation = Input.gyro.attitude;
                _gyroCorrection = Quaternion.Inverse(_initialRotation);
                return true;
            }
            Debug.LogWarning("Gyroscope not supported on this device.");
            return false;
        }

        void Update()
        {
            if (_gyroEnabled)
            {
                RuntimeSettings runtime = GameManager.Instance != null ? GameManager.Instance.CurrentRuntimeSettings : null;
                float sensitivityX = runtime != null ? runtime.GyroSensitivityX : (_settingsData != null ? _settingsData.GyroSensitivityX : 1f);
                float sensitivityY = runtime != null ? runtime.GyroSensitivityY : (_settingsData != null ? _settingsData.GyroSensitivityY : 1f);

                Quaternion currentGyro = Input.gyro.attitude;
                Quaternion deltaRotation = _gyroCorrection * currentGyro;

                // Convert quaternion delta to Euler angles for yaw/pitch
                Vector3 eulerDelta = deltaRotation.eulerAngles;

                // Normalize angles to -180 to 180 range
                float pitch = NormalizeAngle(eulerDelta.x);
                float yaw = NormalizeAngle(eulerDelta.y);

                // Apply sensitivity and clamp angular velocity
                float scaledPitch = Mathf.Clamp(pitch * sensitivityY, -_maxAngularVelocity * Time.deltaTime, _maxAngularVelocity * Time.deltaTime);
                float scaledYaw = Mathf.Clamp(yaw * sensitivityX, -_maxAngularVelocity * Time.deltaTime, _maxAngularVelocity * Time.deltaTime);

                GyroDelta = new Vector2(scaledYaw, scaledPitch);

                // Reset gyro correction periodically or based on user input for recentering
                // For now, we'll assume continuous relative input.
            }
            else
            {
                GyroDelta = Vector2.zero;
            }
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180) angle -= 360;
            while (angle < -180) angle += 360;
            return angle;
        }

        public void RecenterGyro()
        {
            if (_gyroEnabled)
            {
                _initialRotation = Input.gyro.attitude;
                _gyroCorrection = Quaternion.Inverse(_initialRotation);
            }
        }
    }
}
