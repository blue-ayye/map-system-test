using System.Collections.Generic;

namespace BP.MapSystem
{
    /// <summary>
    /// Represents the data structure for storing information about the traversal of a map, including the list of visited nodes, the current node, and the number of traversal steps taken.
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
}