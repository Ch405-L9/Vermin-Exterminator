using UnityEngine;
using System.Collections.Generic;
using BarnSwarmSniper.AI;
using BarnSwarmSniper.Data;

namespace BarnSwarmSniper.Level
{
    public class SpawnZoneManager : MonoBehaviour
    {
        [SerializeField] private RatAIManager _ratAIManager;
        [SerializeField] private int _maxActiveRatsPerZone = 5;
        [SerializeField] private float _spawnInterval = 2f;

        private readonly List<SpawnZone> _spawnZones = new();
        private float _spawnTimer;

        void Start()
        {
            if (_ratAIManager == null)
            {
                Debug.LogError("SpawnZoneManager: RatAIManager not assigned.");
                enabled = false;
            }
        }

        void Update()
        {
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0 && _spawnZones.Count > 0)
            {
                TrySpawnRatsInZones();
                _spawnTimer = _spawnInterval;
            }
        }

        public void ConfigureSpawnZones(Transform[] zones)
        {
            _spawnZones.Clear();
            if (zones != null)
            {
                for (int i = 0; i < zones.Length; i++)
                {
                    if (zones[i] == null)
                    {
                        continue;
                    }

                    var existing = zones[i].GetComponent<SpawnZone>();
                    if (existing != null)
                    {
                        _spawnZones.Add(existing);
                    }
                    else
                    {
                        var fallback = zones[i].gameObject.AddComponent<SpawnZone>();
                        fallback.zoneId = zones[i].name;
                        _spawnZones.Add(fallback);
                    }
                }
            }

            Debug.Log($"Configured {_spawnZones.Count} spawn zones.");
        }

        public void ConfigureSpawnZonesFromTiles(List<GameObject> activeTiles, ContractDefinition contract)
        {
            _spawnZones.Clear();
            if (activeTiles == null || activeTiles.Count == 0)
            {
                return;
            }

            var collected = new List<SpawnZone>();
            for (int i = 0; i < activeTiles.Count; i++)
            {
                var tile = activeTiles[i];
                if (tile == null)
                {
                    continue;
                }

                var zones = tile.GetComponentsInChildren<SpawnZone>(true);
                if (zones != null && zones.Length > 0)
                {
                    collected.AddRange(zones);
                }
            }

            if (collected.Count == 0)
            {
                Debug.LogWarning("SpawnZoneManager: No SpawnZone components found on generated tiles.");
                return;
            }

            // Filter by contract type and sort by priority.
            var filtered = new List<SpawnZone>();
            for (int i = 0; i < collected.Count; i++)
            {
                var zone = collected[i];
                if (zone == null)
                {
                    continue;
                }

                if (PassesContractFilter(zone, contract))
                {
                    filtered.Add(zone);
                }
            }

            if (filtered.Count == 0)
            {
                filtered = collected;
            }

            filtered.Sort((a, b) => b.priority.CompareTo(a.priority));

            int maxZones = Mathf.Clamp(
                4 + (contract != null ? contract.difficultyIndex : 0) * 2,
                2,
                filtered.Count);

            for (int i = 0; i < maxZones; i++)
            {
                _spawnZones.Add(filtered[i]);
            }

            Debug.Log($"Configured {_spawnZones.Count} filtered spawn zones from {collected.Count} collected.");
        }

        private void TrySpawnRatsInZones()
        {
            foreach (SpawnZone zone in _spawnZones)
            {
                if (zone == null)
                {
                    continue;
                }

                // Simple check: if there are less than max rats in this zone's vicinity, spawn one.
                // More sophisticated logic would involve tracking rats per zone.
                if (_ratAIManager.GetActiveRatCount() < _maxActiveRatsPerZone * _spawnZones.Count)
                {
                    float radius = Mathf.Max(0.1f, zone.spawnRadius);
                    _ratAIManager.SpawnRat(zone.transform.position + Random.insideUnitSphere * radius); // Spawn slightly offset
                }
            }
        }

        public void ClearSpawnZones()
        {
            _spawnZones.Clear();
        }

        private static bool PassesContractFilter(SpawnZone zone, ContractDefinition contract)
        {
            if (contract == null || zone.tags == null || zone.tags.Count == 0)
            {
                return true;
            }

            return contract.type switch
            {
                ContractType.NestEradication => zone.tags.Contains("NestPocket") || zone.tags.Contains("UnderMachinery"),
                ContractType.HoldTheLine => zone.tags.Contains("OpenFloor") || zone.tags.Contains("WallEdge"),
                ContractType.CleanSweep => true,
                _ => true
            };
        }
    }
}
