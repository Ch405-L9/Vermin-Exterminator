using UnityEngine;
using BarnSwarmSniper.Data;
using BarnSwarmSniper.Game;
using System.Collections.Generic;

namespace BarnSwarmSniper.Level
{
    public class LevelGenerator : MonoBehaviour
    {
        [SerializeField] private EnvironmentTileLibrary _tileLibrary;
        [SerializeField] private SpawnZoneManager _spawnZoneManager;
        [SerializeField] private LightingModeController _lightingModeController;
        [SerializeField] private LevelSeedGenerator _levelSeedGenerator;
        [SerializeField] private int _defaultTileChainLength = 2;
        [SerializeField] private float _tileSpacing = 32f;

        [Header("Rat Configuration Options")]
        [SerializeField] private RatConfig[] _ratConfigs;

        [System.Serializable]
        public class RatConfig
        {
            public string name;
            public int density;
            public float speedMultiplier;
            public float pauseTimeMultiplier;
        }

        public void GenerateLevel(int playerLevel, string deviceID)
        {
            ContractDefinition contract = null;
            if (GameManager.Instance != null && GameManager.Instance.ContractManager != null)
            {
                contract = GameManager.Instance.ContractManager.SelectedContract;
            }

            GenerateLevel(playerLevel, deviceID, contract);
        }

        public void GenerateLevel(int playerLevel, string deviceID, ContractDefinition contract)
        {
            ClearGeneratedTiles();

            // Generate seed based on player level, date, and device ID
            int seed = _levelSeedGenerator.GenerateSeed(playerLevel, System.DateTime.Now, deviceID);
            Random.InitState(seed);

            int chainLength = Mathf.Clamp(_defaultTileChainLength + (contract != null ? contract.difficultyIndex : 0), 1, 3);
            var activeTileRoots = TryBuildTileChain(contract, chainLength);

            if (activeTileRoots.Count == 0)
            {
                // Fallback to single-tile level for safety.
                GameObject fallback = _tileLibrary.GetRandomTileFromGroup("BarnFloorTile");
                if (fallback != null)
                {
                    activeTileRoots.Add(Instantiate(fallback, Vector3.zero, Quaternion.identity, transform));
                }
            }

            // Choose lighting mode
            _lightingModeController.SetRandomLightingMode();

            // Configure active spawn zones using spawned tiles.
            _spawnZoneManager.ConfigureSpawnZonesFromTiles(activeTileRoots, contract);

            // Choose RatConfig
            RatConfig currentRatConfig = _ratConfigs[Random.Range(0, _ratConfigs.Length)];
            Debug.Log($"Generated Level with Rat Config: {currentRatConfig.name}");

            // Choose objective (placeholder)
            // e.g., time limit or kill count
        }

        private List<GameObject> TryBuildTileChain(ContractDefinition contract, int chainLength)
        {
            var placedTiles = new List<GameObject>();
            var allDescriptors = _tileLibrary.GetAllTileDescriptors();
            if (allDescriptors.Count == 0)
            {
                return placedTiles;
            }

            // Pick start tile using contract tag preference if available.
            var startCandidates = new List<EnvironmentTileDescriptor>();
            if (contract != null && contract.tileTags != null && contract.tileTags.Count > 0)
            {
                for (int i = 0; i < contract.tileTags.Count; i++)
                {
                    startCandidates.AddRange(_tileLibrary.GetTilesWithTag(contract.tileTags[i]));
                }
            }

            if (startCandidates.Count == 0)
            {
                startCandidates = allDescriptors;
            }

            var startDescriptor = startCandidates[Random.Range(0, startCandidates.Count)];
            var startTile = Instantiate(startDescriptor.gameObject, Vector3.zero, Quaternion.identity, transform);
            placedTiles.Add(startTile);

            var currentDescriptor = startDescriptor;
            var currentPosition = Vector3.zero;
            var usedIds = new HashSet<string> { SafeTileId(startDescriptor) };

            for (int step = 1; step < chainLength; step++)
            {
                if (!TrySelectNextConnector(currentDescriptor, out var outgoingDirection, out var connectorType))
                {
                    break;
                }

                var neededSide = Opposite(outgoingDirection);
                var candidates = _tileLibrary.GetTilesByConnector(connectorType, neededSide);
                EnvironmentTileDescriptor chosen = null;

                for (int i = 0; i < candidates.Count; i++)
                {
                    var c = candidates[i];
                    if (c == null)
                    {
                        continue;
                    }

                    // avoid immediate duplicate when possible
                    if (!usedIds.Contains(SafeTileId(c)))
                    {
                        chosen = c;
                        break;
                    }
                }

                if (chosen == null && candidates.Count > 0)
                {
                    chosen = candidates[Random.Range(0, candidates.Count)];
                }

                if (chosen == null)
                {
                    break;
                }

                currentPosition += DirectionToVector(outgoingDirection) * _tileSpacing;
                var tile = Instantiate(chosen.gameObject, currentPosition, Quaternion.identity, transform);
                placedTiles.Add(tile);
                usedIds.Add(SafeTileId(chosen));
                currentDescriptor = chosen;
            }

            return placedTiles;
        }

        private static string SafeTileId(EnvironmentTileDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(descriptor.tileId) ? descriptor.name : descriptor.tileId;
        }

        private static bool TrySelectNextConnector(EnvironmentTileDescriptor descriptor, out TileDirection direction, out TileConnectorType connectorType)
        {
            direction = TileDirection.East;
            connectorType = TileConnectorType.None;

            if (descriptor == null || descriptor.connectors == null || descriptor.connectors.Count == 0)
            {
                return false;
            }

            var valid = new List<TileConnector>();
            for (int i = 0; i < descriptor.connectors.Count; i++)
            {
                var c = descriptor.connectors[i];
                if (c != null && c.connectorType != TileConnectorType.None)
                {
                    valid.Add(c);
                }
            }

            if (valid.Count == 0)
            {
                return false;
            }

            var selected = valid[Random.Range(0, valid.Count)];
            direction = selected.direction;
            connectorType = selected.connectorType;
            return true;
        }

        private static TileDirection Opposite(TileDirection direction)
        {
            return direction switch
            {
                TileDirection.North => TileDirection.South,
                TileDirection.South => TileDirection.North,
                TileDirection.East => TileDirection.West,
                TileDirection.West => TileDirection.East,
                _ => TileDirection.West
            };
        }

        private static Vector3 DirectionToVector(TileDirection direction)
        {
            return direction switch
            {
                TileDirection.North => Vector3.forward,
                TileDirection.South => Vector3.back,
                TileDirection.East => Vector3.right,
                TileDirection.West => Vector3.left,
                _ => Vector3.right
            };
        }

        private void ClearGeneratedTiles()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
