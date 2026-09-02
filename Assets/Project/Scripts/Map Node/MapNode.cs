using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapNode
    {
        public int Level;
        public int Index;
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; } = Vector3.one;

        public List<MapNode> ParentNodes { get; } = new List<MapNode>();
        public List<MapNode> ChildNodes { get; } = new List<MapNode>();
        public IMapNodeView NodeView { get; set; }
        public MapNodeTypeSO NodeType { get; set; }
        public NodeState State { get; set; } = NodeState.Locked;

        public MapNode(int level, int nodeIndex)
        {
            Level = level;
            Index = nodeIndex;
        }
    }
}