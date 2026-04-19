using UnityEngine;

namespace BarnSwarmSniper.AI
{
    public class ScatterBehavior : MonoBehaviour
    {
        // This script can be used to define more complex scattering logic if needed.
        // For now, the basic scatter logic is handled within RatFSM.
        // This script could potentially manage different scatter patterns, speeds, or durations
        // based on external factors (e.g., proximity to player, type of shot).

        public Vector3 GetScatterDirection(Vector3 ratPosition, Vector3 threatPosition)
        {
            Vector3 direction = (ratPosition - threatPosition).normalized;
            direction.y = 0; // Keep scatter on horizontal plane
            return direction;
        }

        public float GetScatterSpeed()
        {
            // Could be dynamic based on rat type or game difficulty
            return 5.0f;
        }

        public float GetScatterDuration()
        {
            // Could be dynamic
            return 0.5f;
        }
    }
}
