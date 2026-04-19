using UnityEngine;

namespace BarnSwarmSniper.Input
{
    public class InputSmoothingPipeline : MonoBehaviour
    {
        [Header("EMA Smoothing Settings")]
        [SerializeField] private float _gyroSmoothingFactor = 0.1f; // 0-1, lower is more smooth
        [SerializeField] private float _touchSmoothingFactor = 0.2f; // 0-1, lower is more smooth

        private Vector2 _smoothedGyroDelta;
        private Vector2 _smoothedTouchDelta;

        public Vector2 GetSmoothedGyroDelta(Vector2 rawGyroDelta)
        {
            _smoothedGyroDelta = Vector2.Lerp(_smoothedGyroDelta, rawGyroDelta, _gyroSmoothingFactor);
            return _smoothedGyroDelta;
        }

        public Vector2 GetSmoothedTouchDelta(Vector2 rawTouchDelta)
        {
            _smoothedTouchDelta = Vector2.Lerp(_smoothedTouchDelta, rawTouchDelta, _touchSmoothingFactor);
            return _smoothedTouchDelta;
        }

        public void ResetSmoothing()
        {
            _smoothedGyroDelta = Vector2.zero;
            _smoothedTouchDelta = Vector2.zero;
        }
    }
}
