using UnityEngine;

namespace BarnSwarmSniper.Camera
{
    public class BreathingSway : MonoBehaviour
    {
        [SerializeField] private float _swayMagnitude = 0.05f; // How much the camera sways
        [SerializeField] private float _swaySpeed = 1.5f; // How fast the camera sways
        [SerializeField] private bool _enableSway = true;

        private Vector3 _initialLocalPosition;

        void Start()
        {
            _initialLocalPosition = transform.localPosition;
        }

        void Update()
        {
            if (_enableSway)
            {
                float xSway = Mathf.Sin(Time.time * _swaySpeed) * _swayMagnitude;
                float ySway = Mathf.Cos(Time.time * _swaySpeed * 0.7f) * _swayMagnitude * 0.5f;

                transform.localPosition = _initialLocalPosition + new Vector3(xSway, ySway, 0f);
            }
            else
            {
                transform.localPosition = _initialLocalPosition;
            }
        }

        public void SetSwayEnabled(bool enabled)
        {
            _enableSway = enabled;
        }
    }
}
