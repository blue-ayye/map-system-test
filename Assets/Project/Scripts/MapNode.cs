using System.Collections.Generic;

namespace BP.MapGeneration
{
    /// <summary>
    /// Pure data class representing a node in the map generation process. Helps with performance by avoiding instantiating view objects during path generation.
    /// </summary>
    [System.Serializable]
    public class MapNode
    {
        public int Level;
        public int NodeIndex;
        public List<MapNode> ParentNodes = new List<MapNode>();
        public List<MapNode> ChildNodes = new List<MapNode>();

        public MapNode(int level, int nodeIndex)
        {
            Level = level;
            NodeIndex = nodeIndex;
        }
    }
}