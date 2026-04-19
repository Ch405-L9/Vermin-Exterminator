using UnityEngine;
using System.Collections.Generic;

namespace BarnSwarmSniper.AI
{
    public class MovementPathfinder : MonoBehaviour
    {
        // This script will be responsible for providing valid movement paths or target points for rats.
        // In a more complex game, this would involve NavMesh or custom graph-based pathfinding.
        // For the initial prototype, we can provide simple random points within a defined area.

        [SerializeField] private LayerMask _walkableLayer; // Layer for environment tiles rats can walk on
        [SerializeField] private float _searchRadius = 10f; // Radius to search for valid movement points

        // A simple method to get a random walkable point within a certain radius
        public Vector3 GetRandomWalkablePoint(Vector3 center, float radius)
        {
            for (int i = 0; i < 10; i++) // Try a few times to find a valid point
            {
                Vector3 randomPoint = center + Random.insideUnitSphere * radius;
                randomPoint.y = center.y; // Keep on the same horizontal plane for simplicity

                // Check if the point is on a walkable surface (e.g., using a raycast down)
                RaycastHit hit;
                if (Physics.Raycast(randomPoint + Vector3.up * 5f, Vector3.down, out hit, 10f, _walkableLayer))
                {
                    return hit.point; // Return the point on the walkable surface
                }
            }
            return center; // If no walkable point found, return the center
        }

        // In a more advanced setup, this could return a series of waypoints
        public List<Vector3> GetPath(Vector3 start, Vector3 end)
        {
            // For now, a direct path. Later, implement actual pathfinding.
            return new List<Vector3> { end };
        }
    }
}
