using UnityEngine;

namespace BarnSwarmSniper.Data
{
    [CreateAssetMenu(fileName = "SettingsData", menuName = "BarnSwarmSniper/Settings Data", order = 1)]
    public class SettingsData : ScriptableObject
    {
        [Header("Input Settings")]
        public float GyroSensitivityX = 1.0f;
        public float GyroSensitivityY = 1.0f;
        public float TouchSensitivity = 0.5f;

        [Header("Aim Assist Settings")]
        public float AimAssistFrictionRadius = 0.1f; // Radius around rat collider for reticle friction
        public float AimAssistFrictionStrength = 0.5f; // How strong the friction is
        public float AimAssistMagnetismStrength = 0.2f; // How strong the magnetism is when firing

        [Header("Camera Settings")]
        public bool BreathingSwayEnabled = true;

        [Header("UI Settings")]
        public bool IsRightHandedLayout = true; // True for right-handed, false for left-handed (UI mirroring)
    }
}
