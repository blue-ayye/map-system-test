using System.Collections.Generic;

namespace BP.MapSystem
{
    /// <summary>
    /// Pure data class representing a node in the map generation process. Helps with performance by avoiding instantiating view objects during path generation.
    /// </summary>
    ///
    public class MapNode
    {
        public int Level;
        public int NodeIndex;
        public List<MapNode> ParentNodes { get; } = new List<MapNode>();
        public List<MapNode> ChildNodes { get; } = new List<MapNode>();

        public MapNode(int level, int nodeIndex)
        {
            Level = level;
            NodeIndex = nodeIndex;
        }
    }
}