using UnityEngine;
using System.Collections.Generic;

namespace BarnSwarmSniper.Pooling
{
    public class RatObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject _ratPrefab;
        [SerializeField] private int _poolSize = 60;

        private Queue<AI.RatFSM> _ratPool = new Queue<AI.RatFSM>();

        void Awake()
        {
            PrewarmPool();
        }

        private void PrewarmPool()
        {
            if (_ratPrefab == null)
            {
                Debug.LogError("RatObjectPool: Rat Prefab not assigned.");
                return;
            }

            for (int i = 0; i < _poolSize; i++)
            {
                GameObject ratGO = Instantiate(_ratPrefab, transform);
                AI.RatFSM ratFSM = ratGO.GetComponent<AI.RatFSM>();
                if (ratFSM == null)
                {
                    Debug.LogError("RatObjectPool: Rat Prefab does not have a RatFSM component.");
                    Destroy(ratGO);
                    continue;
                }
                ratGO.SetActive(false);
                _ratPool.Enqueue(ratFSM);
            }
        }

        public AI.RatFSM GetRat()
        {
            if (_ratPool.Count > 0)
            {
                AI.RatFSM rat = _ratPool.Dequeue();
                // rat.gameObject.SetActive(true); // Activated by RatFSM.InitializeAndActivate
                return rat;
            }
            else
            {
                Debug.LogWarning("RatObjectPool: Pool is empty, consider increasing pool size.");
                // Optionally, instantiate a new one if pool is exhausted (but avoid during gameplay)
                GameObject ratGO = Instantiate(_ratPrefab, transform);
                AI.RatFSM ratFSM = ratGO.GetComponent<AI.RatFSM>();
                if (ratFSM == null)
                {
                    Debug.LogError("RatObjectPool: Newly instantiated Rat Prefab does not have a RatFSM component.");
                    Destroy(ratGO);
                    return null;
                }
                return ratFSM;
            }
        }

        public void ReturnRat(AI.RatFSM rat)
        {
            rat.DeactivateAndPool(); // Deactivates and sets state to Pooled
            _ratPool.Enqueue(rat);
        }
    }
}
