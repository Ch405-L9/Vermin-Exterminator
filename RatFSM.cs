using UnityEngine;

namespace BarnSwarmSniper.AI
{
    public class RatFSM : MonoBehaviour
    {
        public enum RatState { Idle, Move, Pause, Scatter, Dead, Pooled }
        public RatState CurrentState { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeedMin = 1.0f;
        [SerializeField] private float _moveSpeedMax = 3.0f;
        [SerializeField] private float _burstMoveDurationMin = 0.3f;
        [SerializeField] private float _burstMoveDurationMax = 1.0f;
        [SerializeField] private float _pauseDurationMin = 0.2f;
        [SerializeField] private float _pauseDurationMax = 0.6f;
        [SerializeField] private float _scatterSpeed = 5.0f;
        [SerializeField] private float _scatterDuration = 0.5f;

        private Vector3 _targetPosition;
        private float _currentMoveSpeed;
        private float _stateTimer;
        private Vector3 _scatterDirection;

        // Components
        private CharacterController _characterController;
        private Animator _animator;

        void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            CurrentState = RatState.Pooled; // Start in pooled state
        }

        public void InitializeAndActivate(Vector3 startPosition)
        {
            transform.position = startPosition;
            gameObject.SetActive(true);
            TransitionToState(RatState.Idle);
        }

        public void DeactivateAndPool()
        {
            gameObject.SetActive(false);
            CurrentState = RatState.Pooled;
        }

        void Update()
        {
            _stateTimer -= Time.deltaTime;

            switch (CurrentState)
            {
                case RatState.Idle:
                    if (_stateTimer <= 0)
                    {
                        TransitionToState(RatState.Move);
                    }
                    break;
                case RatState.Move:
                    MoveRat();
                    if (_stateTimer <= 0)
                    {
                        TransitionToState(RatState.Pause);
                    }
                    break;
                case RatState.Pause:
                    if (_stateTimer <= 0)
                    {
                        TransitionToState(RatState.Move);
                    }
                    break;
                case RatState.Scatter:
                    ScatterRat();
                    if (_stateTimer <= 0)
                    {
                        TransitionToState(RatState.Idle);
                    }
                    break;
                case RatState.Dead:
                    // Rat is dead, might play death animation or just stay still
                    break;
                case RatState.Pooled:
                    // Do nothing, waiting to be activated
                    break;
            }
        }

        private void TransitionToState(RatState newState)
        {
            CurrentState = newState;
            switch (newState)
            {
                case RatState.Idle:
                    _stateTimer = Random.Range(_pauseDurationMin, _pauseDurationMax);
                    if (_animator != null) _animator.SetTrigger("Idle");
                    break;
                case RatState.Move:
                    _stateTimer = Random.Range(_burstMoveDurationMin, _burstMoveDurationMax);
                    _currentMoveSpeed = Random.Range(_moveSpeedMin, _moveSpeedMax);
                    // TODO: Get a new target position from MovementPathfinder or LevelGenerator
                    // For now, a simple random direction
                    _targetPosition = transform.position + Random.insideUnitSphere * 5f;
                    _targetPosition.y = transform.position.y; // Keep on ground plane
                    if (_animator != null) _animator.SetTrigger("Move");
                    break;
                case RatState.Pause:
                    _stateTimer = Random.Range(_pauseDurationMin, _pauseDurationMax);
                    if (_animator != null) _animator.SetTrigger("Pause");
                    break;
                case RatState.Scatter:
                    _stateTimer = _scatterDuration;
                    _scatterDirection = (transform.position - Camera.main.transform.position).normalized; // Scatter away from camera
                    _scatterDirection.y = 0; // Keep scatter on horizontal plane
                    if (_animator != null) _animator.SetTrigger("Scatter");
                    break;
                case RatState.Dead:
                    // Play death animation, disable collider, etc.
                    if (_animator != null) _animator.SetTrigger("Die");
                    // Optionally, schedule pooling after death animation
                    break;
            }
        }

        private void MoveRat()
        {
            if (_characterController != null)
            {
                Vector3 direction = (_targetPosition - transform.position).normalized;
                _characterController.Move(direction * _currentMoveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
                {
                    TransitionToState(RatState.Pause);
                }
            }
        }

        private void ScatterRat()
        {
            if (_characterController != null)
            {
                _characterController.Move(_scatterDirection * _scatterSpeed * Time.deltaTime);
            }
        }

        public void OnShot()
        {
            TransitionToState(RatState.Dead);
            // Notify RatAIManager or ScoreManager
        }
    }
}
