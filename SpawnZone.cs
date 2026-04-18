using System.Collections.Generic;
using UnityEngine;

namespace BarnSwarmSniper.Level
{
    public class SpawnZone : MonoBehaviour
    {
        public string zoneId;
        public List<string> tags = new();
        public int priority = 0;
        public float spawnRadius = 2f;
    }
}

