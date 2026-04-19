using System;

namespace BarnSwarmSniper.Data
{
    [Serializable]
    public class RuntimeSettings
    {
        public float GyroSensitivityX = 1.0f;
        public float GyroSensitivityY = 1.0f;
        public float TouchSensitivity = 0.5f;

        public float AimAssistFrictionRadius = 0.1f;
        public float AimAssistFrictionStrength = 0.5f;
        public float AimAssistMagnetismStrength = 0.2f;

        public bool BreathingSwayEnabled = true;
        public bool IsRightHandedLayout = true;

        public static RuntimeSettings FromSettingsData(SettingsData source)
        {
            var runtime = new RuntimeSettings();
            if (source == null)
            {
                return runtime;
            }

            runtime.GyroSensitivityX = source.GyroSensitivityX;
            runtime.GyroSensitivityY = source.GyroSensitivityY;
            runtime.TouchSensitivity = source.TouchSensitivity;
            runtime.AimAssistFrictionRadius = source.AimAssistFrictionRadius;
            runtime.AimAssistFrictionStrength = source.AimAssistFrictionStrength;
            runtime.AimAssistMagnetismStrength = source.AimAssistMagnetismStrength;
            runtime.BreathingSwayEnabled = source.BreathingSwayEnabled;
            runtime.IsRightHandedLayout = source.IsRightHandedLayout;
            return runtime;
        }
    }
}

