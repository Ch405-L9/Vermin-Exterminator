using UnityEngine;
using BarnSwarmSniper.Data;
using BarnSwarmSniper.Game;

namespace BarnSwarmSniper.Input
{
    public class TouchInputHandler : MonoBehaviour
    {
        [SerializeField] private SettingsData _settingsData;
        [SerializeField] private float _rightSideThreshold = 0.5f; // Percentage of screen width for right-side touch

        public Vector2 TouchDelta { get; private set; }
        public bool IsFiring { get; private set; }

        private Vector2 _lastTouchPosition;

        void Update()
        {
            TouchDelta = Vector2.zero;
            IsFiring = false;
            RuntimeSettings runtime = GameManager.Instance != null ? GameManager.Instance.CurrentRuntimeSettings : null;
            float touchSensitivity = runtime != null ? runtime.TouchSensitivity : (_settingsData != null ? _settingsData.TouchSensitivity : 0.5f);

            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);

                // Check for right-side drag for aiming
                if (touch.position.x > Screen.width * _rightSideThreshold)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        _lastTouchPosition = touch.position;
                    }
                    else if (touch.phase == TouchPhase.Moved)
                    {
                        Vector2 currentTouchPosition = touch.position;
                        TouchDelta = (currentTouchPosition - _lastTouchPosition) * touchSensitivity;
                        _lastTouchPosition = currentTouchPosition;
                    }
                    else if (touch.phase == TouchPhase.Ended && touch.tapCount == 1)
                    {
                        IsFiring = true;
                    }
                }
            }
        }
    }
}
