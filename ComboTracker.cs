using UnityEngine;

namespace BarnSwarmSniper.Scoring
{
    public class ComboTracker : MonoBehaviour
    {
        [SerializeField] private float _comboTimeout = 2.0f; // Time in seconds before combo resets

        public int CurrentCombo { get; private set; }
        public event System.Action<int> OnComboChanged;
        public event System.Action OnComboReset;

        private float _comboTimer;

        void Start()
        {
            CurrentCombo = 0;
            _comboTimer = _comboTimeout;
        }

        void Update()
        {
            if (CurrentCombo > 0)
            {
                _comboTimer -= Time.deltaTime;
                if (_comboTimer <= 0)
                {
                    ResetCombo();
                }
            }
        }

        public void IncrementCombo()
        {
            CurrentCombo++;
            _comboTimer = _comboTimeout; // Reset timer on successful hit
            OnComboChanged?.Invoke(CurrentCombo);
        }

        public void ResetCombo()
        {
            if (CurrentCombo > 0)
            {
                CurrentCombo = 0;
                OnComboReset?.Invoke();
                OnComboChanged?.Invoke(CurrentCombo);
            }
        }
    }
}
