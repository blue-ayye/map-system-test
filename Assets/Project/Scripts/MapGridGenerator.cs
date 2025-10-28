using UnityEngine;

namespace BP.MapGeneration
{
    public class MapGridGenerator : MonoBehaviour
    {
        [SerializeField] private int _maxLevels = 9;
        [SerializeField] private int _nodesPerLevel = 7;
        [SerializeField] private Transform _nodeViewParent;
        [SerializeField] private Transform _nodeViewPrefab;

        public MapNode[,] MapGrid;

        // Grid management
        public void ClearUnusedNodes()
        {
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = MapGrid[level, nodeIndex];
                    if (node.ParentNodes.Count == 0 && node.ChildNodes.Count == 0)
                    {
                        MapGrid[level, nodeIndex] = null;
                    }
                }
            }
        }

        public void ClearNodeViews()
        {
            foreach (Transform child in _nodeViewParent)
            {
                Destroy(child.gameObject);
            }
        }

        public void CreateNodeViews()
        {
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = MapGrid[level, nodeIndex];
                    if (node == null) continue;

                    Vector3 position = new Vector3(nodeIndex * 2.0f, level * 2.0f, 0);
                    var nodeView = Instantiate(_nodeViewPrefab, _nodeViewParent);
                    nodeView.position = position;
                }
            }
        }

        public void CreateNodeGrid()
        {
            MapGrid = new MapNode[_maxLevels, _nodesPerLevel];
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = new MapNode(level, nodeIndex);
                    MapGrid[level, nodeIndex] = node;
                }
            }
        }
    }
}