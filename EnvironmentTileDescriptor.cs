using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarnSwarmSniper.Level
{
    public enum TileConnectorType
    {
        None,
        BarnDoor,
        Corridor,
        Crawlspace,
        LoftEdge,
        UnderFloorGap,
        YardExit
    }

    public enum TileDirection
    {
        North,
        South,
        East,
        West
    }

    [Serializable]
    public class TileConnector
    {
        public TileDirection direction;
        public TileConnectorType connectorType;
    }

    public class EnvironmentTileDescriptor : MonoBehaviour
    {
        public string tileId;
        public List<string> tags = new();
        public List<TileConnector> connectors = new();
    }
}

