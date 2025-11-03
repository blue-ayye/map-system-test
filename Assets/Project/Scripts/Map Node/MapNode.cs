using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BP.MapSystem
{
    /// <summary>
    /// Pure data class representing a node in the map generation process. Helps with performance by avoiding instantiating view objects during path generation.
    /// </summary>
    ///
    public class MapNode
    {
        public int Level;
        public int Index;
        public List<MapNode> ParentNodes { get; } = new List<MapNode>();
        public List<MapNode> ChildNodes { get; } = new List<MapNode>();
        public IMapNodeView NodeView { get; set; }
        public MapNodeTypeSO NodeType { get; set; }
        public Vector3 Position => NodeView != null ? NodeView.Transform.position : Vector3.zero;

        public MapNode(int level, int nodeIndex)
        {
            Level = level;
            Index = nodeIndex;
        }
    }
}