using UnityEngine;
using System.IO;

namespace BarnSwarmSniper.Data
{
    public class SettingsManager : MonoBehaviour
    {
        private string _savePath;
        public SettingsData CurrentSettings { get; private set; }

        void Awake()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "gameSettings.json");
            LoadSettings();
        }

        public void LoadSettings()
        {
            if (File.Exists(_savePath))
            {
                string json = File.ReadAllText(_savePath);
                CurrentSettings = JsonUtility.FromJson<SettingsData>(json);
                Debug.Log("Game settings loaded.");
            }
            else
            {
                CurrentSettings = ScriptableObject.CreateInstance<SettingsData>(); // Create a new instance
                Debug.Log("No game settings found, creating new one with defaults.");
                SaveSettings(); // Save the newly created settings
            }
        }

        public void SaveSettings()
        {
            string json = JsonUtility.ToJson(CurrentSettings);
            File.WriteAllText(_savePath, json);
            Debug.Log("Game settings saved.");
        }

        // Methods to update specific settings
        public void UpdateGyroSensitivityX(float value)
        {
            CurrentSettings.GyroSensitivityX = value;
            SaveSettings();
        }

        public void UpdateGyroSensitivityY(float value)
        {
            CurrentSettings.GyroSensitivityY = value;
            SaveSettings();
        }

        public void UpdateTouchSensitivity(float value)
        {
            CurrentSettings.TouchSensitivity = value;
            SaveSettings();
        }

        public void UpdateAimAssist(float frictionRadius, float frictionStrength, float magnetismStrength)
        {
            CurrentSettings.AimAssistFrictionRadius = frictionRadius;
            CurrentSettings.AimAssistFrictionStrength = frictionStrength;
            CurrentSettings.AimAssistMagnetismStrength = magnetismStrength;
            SaveSettings();
        }

        public void UpdateBreathingSway(bool enabled)
        {
            CurrentSettings.BreathingSwayEnabled = enabled;
            SaveSettings();
        }

        public void UpdateLayout(bool rightHanded) // Assuming true for right-handed, false for left
        {
            CurrentSettings.IsRightHandedLayout = rightHanded;
            SaveSettings();
        }
    }
}
