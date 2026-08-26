using System.Collections.Generic;

namespace BP.MapSystem
{
    #region Grid Parameters

    /// <summary>
    /// Grid configuration parameters persisted alongside seed data so a map can be
    /// reproduced identically even if the default inspector values change between versions.
    /// </summary>
    [System.Serializable]
    public struct GridParameters
    {
        public int MaxLevels;
        public int NodesPerLevel;
        public float NodeSpaceJitterAmount;
        public float LevelSpaceJitterAmount;
    }

    #endregion

    #region Save Data Classes

    /// <summary>
    /// Top-level save payload. Increment <see cref="Version"/> whenever the schema changes
    /// to support forward/backward compatibility checks.
    /// </summary>
    [System.Serializable]
    public class MapData
    {
        public int Version = 1;
        public int Seed;
        public bool IsCustomSeedUsed;
        public GridParameters GridParameters;
        public List<MapNodeData> MapNodeDataList;
        public MapTraversalData MapTraversalData;
    }

    /// <summary>
    /// Serializable snapshot of a single map node (position in the grid and its assigned type).
    /// </summary>
    [System.Serializable]
    public class MapNodeData
    {
        public int Level;
        public int Index;
        public string NodeTypeID;

        public MapNodeData(MapNode node)
        {
            Level = node.Level;
            Index = node.Index;
            NodeTypeID = node.NodeType.ID;
        }
    }

    /// <summary>
    /// Serializable snapshot of the player's traversal progress through the map.
    /// <see cref="CurrentNodeData"/> is <c>null</c> when the player has not yet selected
    /// a starting node.
    /// </summary>
    [System.Serializable]
    public class MapTraversalData
    {
        public List<MapNodeData> VisitedNodeDataList = new List<MapNodeData>();
        public MapNodeData CurrentNodeData = null;
        public int TraversalStepsTaken;

        public MapTraversalData(List<MapNode> visitedNodes, MapNode currentNode, int stepsTaken)
        {
            foreach (var node in visitedNodes)
            {
                VisitedNodeDataList.Add(new MapNodeData(node));
            }

            if (currentNode != null)
            {
                CurrentNodeData = new MapNodeData(currentNode);
            }

            TraversalStepsTaken = stepsTaken;
        }
    }

    #endregion
}
