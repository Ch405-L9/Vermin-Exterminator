using UnityEngine;

namespace BarnSwarmSniper.Weapon
{
    public class RecoilSystem : MonoBehaviour
    {
        [SerializeField] private float _recoilAmount = 0.1f; // How much the camera kicks up
        [SerializeField] private float _recoilSpeed = 10f; // How fast the recoil happens
        [SerializeField] private float _recoverySpeed = 5f; // How fast the camera recovers

        private float _baseRecoilAmount;
        private float _baseRecoverySpeed;

        private Vector3 _currentRecoil = Vector3.zero;
        private Vector3 _recoilVelocity = Vector3.zero;

        public Quaternion CurrentRecoilRotation { get; private set; }

        private void Awake()
        {
            _baseRecoilAmount = _recoilAmount;
            _baseRecoverySpeed = _recoverySpeed;
        }

        void Update()
        {
            // Apply recoil
            _currentRecoil = Vector3.SmoothDamp(_currentRecoil, Vector3.zero, ref _recoilVelocity, 1 / _recoverySpeed);
            CurrentRecoilRotation = Quaternion.Euler(_currentRecoil);
        }

        public void AddRecoil(Vector3 recoilForce)
        {
            _currentRecoil += recoilForce * _recoilAmount;
        }

        public void ApplyMultipliers(float recoilAmountMultiplier, float recoilRecoveryMultiplier)
        {
            _recoilAmount = Mathf.Max(0.0001f, _baseRecoilAmount * Mathf.Max(0.0001f, recoilAmountMultiplier));
            _recoverySpeed = Mathf.Max(0.01f, _baseRecoverySpeed * Mathf.Max(0.0001f, recoilRecoveryMultiplier));
        }
    }
}
