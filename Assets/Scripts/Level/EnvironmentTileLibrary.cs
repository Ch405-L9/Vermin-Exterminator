using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BarnSwarmSniper.Level
{
    [CreateAssetMenu(fileName = "EnvironmentTileLibrary", menuName = "BarnSwarmSniper/Environment Tile Library", order = 2)]
    public class EnvironmentTileLibrary : ScriptableObject
    {
        [System.Serializable]
        public class TileGroup
        {
            public string groupName;
            public List<GameObject> tiles;
        }

        public List<TileGroup> tileGroups;

        public List<EnvironmentTileDescriptor> GetAllTileDescriptors()
        {
            var list = new List<EnvironmentTileDescriptor>();
            if (tileGroups == null)
            {
                return list;
            }

            for (int i = 0; i < tileGroups.Count; i++)
            {
                var group = tileGroups[i];
                if (group == null || group.tiles == null)
                {
                    continue;
                }

                for (int j = 0; j < group.tiles.Count; j++)
                {
                    var tilePrefab = group.tiles[j];
                    if (tilePrefab == null)
                    {
                        continue;
                    }

                    var descriptor = tilePrefab.GetComponent<EnvironmentTileDescriptor>();
                    if (descriptor != null)
                    {
                        list.Add(descriptor);
                    }
                }
            }

            return list;
        }

        public List<EnvironmentTileDescriptor> GetTilesWithTag(string tag)
        {
            var all = GetAllTileDescriptors();
            if (string.IsNullOrWhiteSpace(tag))
            {
                return all;
            }

            return all.Where(d => d.tags != null && d.tags.Contains(tag)).ToList();
        }

        public List<EnvironmentTileDescriptor> GetTilesByConnector(TileConnectorType connectorType, TileDirection direction)
        {
            var all = GetAllTileDescriptors();
            var result = new List<EnvironmentTileDescriptor>();

            for (int i = 0; i < all.Count; i++)
            {
                var descriptor = all[i];
                if (descriptor.connectors == null)
                {
                    continue;
                }

                for (int c = 0; c < descriptor.connectors.Count; c++)
                {
                    var conn = descriptor.connectors[c];
                    if (conn != null && conn.connectorType == connectorType && conn.direction == direction)
                    {
                        result.Add(descriptor);
                        break;
                    }
                }
            }

            return result;
        }

        public GameObject GetRandomTileFromGroup(string groupName)
        {
            foreach (TileGroup group in tileGroups)
            {
                if (group.groupName == groupName && group.tiles.Count > 0)
                {
                    return group.tiles[Random.Range(0, group.tiles.Count)];
                }
            }
            Debug.LogWarning($"No tiles found for group: {groupName}");
            return null;
        }

        public GameObject GetRandomTilePrefabFromDescriptors(List<EnvironmentTileDescriptor> descriptors)
        {
            if (descriptors == null || descriptors.Count == 0)
            {
                return null;
            }

            int idx = Random.Range(0, descriptors.Count);
            return descriptors[idx] != null ? descriptors[idx].gameObject : null;
        }
    }
}
