using UnityEngine;
using System.Collections;
using BarnSwarmSniper.Pooling;

namespace BarnSwarmSniper.UI
{
    public class HitMarkerFeedback : MonoBehaviour
    {
        [SerializeField] private EffectsObjectPool _effectsObjectPool;
        [SerializeField] private float _displayDuration = 0.5f;

        public void ShowHitMarker(Vector3 position, Quaternion rotation)
        {
            if (_effectsObjectPool != null)
            {
                GameObject hitMarker = _effectsObjectPool.GetHitMarker(position, rotation);
                StartCoroutine(ReturnHitMarkerAfterDelay(hitMarker, _displayDuration));
            }
        }

        private System.Collections.IEnumerator ReturnHitMarkerAfterDelay(GameObject hitMarker, float delay)
        {
            yield return new WaitForSeconds(delay);
            _effectsObjectPool.ReturnHitMarker(hitMarker);
        }
    }
}
