using System.Collections.Generic;

namespace BP.MapSystem
{
    [System.Serializable]
    public class PathData
    {
        public MapNodeData FromNodeData;
        public MapNodeData ToNodeData;

        public PathData(MapNode from, MapNode to)
        {
            FromNodeData = new MapNodeData(from);
            ToNodeData = new MapNodeData(to);
        }
    }

    [System.Serializable]
    public class MapTraversalData
    {
        public List<PathData> TraversedPathDataList = new List<PathData>();
        public MapNodeData CurrentNodeData = null;
        public int TraversalStepsTaken;

        public MapTraversalData(List<(MapNode From, MapNode To)> traversedEdges, MapNode currentNode, int stepsTaken)
        {
            foreach (var edge in traversedEdges)
            {
                TraversedPathDataList.Add(new PathData(edge.From, edge.To));
            }

            if (currentNode != null)
            {
                CurrentNodeData = new MapNodeData(currentNode);
            }

            TraversalStepsTaken = stepsTaken;
        }
    }
}