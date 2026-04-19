using UnityEngine;
using System.Collections.Generic;

namespace BarnSwarmSniper.Pooling
{
    public class EffectsObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject _hitMarkerPrefab;
        [SerializeField] private GameObject _impactDustPrefab;
        [SerializeField] private int _poolSize = 10;

        private Queue<GameObject> _hitMarkerPool = new Queue<GameObject>();
        private Queue<GameObject> _impactDustPool = new Queue<GameObject>();

        void Awake()
        {
            PrewarmPool(_hitMarkerPrefab, _hitMarkerPool);
            PrewarmPool(_impactDustPrefab, _impactDustPool);
        }

        private void PrewarmPool(GameObject prefab, Queue<GameObject> pool)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"EffectsObjectPool: Prefab not assigned for pool {pool.GetType().Name}.");
                return;
            }

            for (int i = 0; i < _poolSize; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        public GameObject GetHitMarker(Vector3 position, Quaternion rotation)
        {
            return GetPooledObject(_hitMarkerPool, _hitMarkerPrefab, position, rotation);
        }

        public void ReturnHitMarker(GameObject obj)
        {
            ReturnPooledObject(obj, _hitMarkerPool);
        }

        public GameObject GetImpactDust(Vector3 position, Quaternion rotation)
        {
            return GetPooledObject(_impactDustPool, _impactDustPrefab, position, rotation);
        }

        public void ReturnImpactDust(GameObject obj)
        {
            ReturnPooledObject(obj, _impactDustPool);
        }

        private GameObject GetPooledObject(Queue<GameObject> pool, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject obj;
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                Debug.LogWarning($"EffectsObjectPool: Pool for {prefab.name} is empty, instantiating new one.");
                obj = Instantiate(prefab, transform);
            }
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }

        private void ReturnPooledObject(GameObject obj, Queue<GameObject> pool)
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}
