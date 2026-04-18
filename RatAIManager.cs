using UnityEngine;
using System.Collections.Generic;
using BarnSwarmSniper.Pooling;

namespace BarnSwarmSniper.AI
{
    public class RatAIManager : MonoBehaviour
    {
        [SerializeField] private RatObjectPool _ratObjectPool;
        [SerializeField] private int _maxActiveRats = 20;

        private List<RatFSM> _activeRats = new List<RatFSM>();

        public event System.Action<RatFSM> OnRatKilled;

        void Start()
        {
            if (_ratObjectPool == null)
            {
                Debug.LogError("RatAIManager: RatObjectPool not assigned.");
                enabled = false;
            }
        }

        public void SpawnRat(Vector3 position)
        {
            if (_activeRats.Count < _maxActiveRats)
            {
                RatFSM rat = _ratObjectPool.GetRat();
                if (rat != null)
                {
                    rat.InitializeAndActivate(position);
                    _activeRats.Add(rat);
                }
            }
        }

        public void DespawnRat(RatFSM rat)
        {
            if (_activeRats.Contains(rat))
            {
                _activeRats.Remove(rat);
                rat.DeactivateAndPool();
                OnRatKilled?.Invoke(rat); // Notify listeners that a rat was killed
            }
        }

        public void ClearAllRats()
        {
            foreach (RatFSM rat in _activeRats)
            {
                rat.DeactivateAndPool();
            }
            _activeRats.Clear();
        }

        public int GetActiveRatCount()
        {
            return _activeRats.Count;
        }
    }
}
