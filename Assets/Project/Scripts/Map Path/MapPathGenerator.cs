using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapPathGenerator : MonoBehaviour
    {
        [Header("Path Generation Settings")]
        [SerializeField] private int _uniquePaths = 3;
        [SerializeField] private int _totalPaths = 7;

        [Header("Path View Settings")]
        [SerializeField] private Transform _pathViewParent;
        [SerializeField] private Transform _pathViewPrefab;

        private MapNode[,] _mapGrid;
        private int _maxLevels;
        private int _nodesPerLevel;
        private System.Random _pathingRNG;
        private Dictionary<int, List<MapNode>> _generatedPaths = new Dictionary<int, List<MapNode>>();

        public List<IMapPathView> PathViews { get; private set; } = new List<IMapPathView>();

        #region Public APIs

        public void Initialize(MapNode[,] mapGrid, System.Random pathingRNG)
        {
            _mapGrid = mapGrid;
            _maxLevels = mapGrid.GetLength(0);
            _nodesPerLevel = mapGrid.GetLength(1);
            _pathingRNG = pathingRNG;
        }

        public void SelectStartingNodes()
        {
            int startingLevel = 0;
            _generatedPaths.Clear();

            // Ensure at least <_uniquePaths> number of nodes are unique
            for (int pathIndex = 0; pathIndex < _uniquePaths; pathIndex++)
            {
                MapNode randomNode;
                _generatedPaths[pathIndex] = new List<MapNode>();
                do
                {
                    int randomIndex = _pathingRNG.Next(0, _nodesPerLevel);
                    randomNode = _mapGrid[startingLevel, randomIndex];
                } while (_generatedPaths[pathIndex].Contains(randomNode));

                _generatedPaths[pathIndex].Add(randomNode);
            }

            // Then fill the rest allowing duplicate nodes for multiple paths from the same starting point
            while (_generatedPaths.Count < _totalPaths)
            {
                int randomIndex = _pathingRNG.Next(0, _nodesPerLevel);
                var randomNode = _mapGrid[startingLevel, randomIndex];
                int pathIndex = _generatedPaths.Count;

                if (!_generatedPaths.ContainsKey(pathIndex))
                {
                    _generatedPaths[pathIndex] = new List<MapNode>();
                }

                _generatedPaths[pathIndex].Add(randomNode);
            }
        }

        public void GeneratePaths()
        {
            foreach (var pathEntry in _generatedPaths)
            {
                List<MapNode> pathNodes = pathEntry.Value;
                MapNode currentNode = pathNodes[0];

                for (int level = 0; level < _maxLevels - 1; level++)
                {
                    var nextNode = GetValidNextNode(currentNode, level + 1);
                    if (nextNode == null) break; // No valid next node, end this path

                    LinkNodes(nextNode, currentNode);
                    currentNode = nextNode;
                    pathNodes.Add(currentNode);
                }
            }
        }

        public void CreatePathViews()
        {
            // OPTIMIZATION: Use HashSet to prevent overlapping duplicate lines
            HashSet<(MapNode, MapNode)> drawnPaths = new HashSet<(MapNode, MapNode)>();

            foreach (var pathNodes in _generatedPaths.Values)
            {
                for (int i = 0; i < pathNodes.Count - 1; i++)
                {
                    var fromNode = pathNodes[i];
                    var toNode = pathNodes[i + 1];

                    // If we haven't drawn this connection yet
                    if (drawnPaths.Add((fromNode, toNode)))
                    {
                        var pathViewTransform = Instantiate(_pathViewPrefab, _pathViewParent);
                        pathViewTransform.localPosition = Vector3.zero;

                        if (pathViewTransform.TryGetComponent(out IMapPathView pathView))
                        {
                            pathView.SetupPath(fromNode, toNode);
                            PathViews.Add(pathView);
                        }
                    }
                }
            }
        }

        public void ClearPathViews()
        {
            foreach (Transform child in _pathViewParent)
            {
                Destroy(child.gameObject);
            }

            PathViews.Clear();
        }

        #endregion Public APIs

        #region Pathing Logic

        private MapNode GetValidNextNode(MapNode currentNode, int nextLevel)
        {
            List<MapNode> potentialNextNodes = new List<MapNode>();
            int currentIndex = currentNode.Index;

            for (int offset = -1; offset <= 1; offset++) // Implements Rule 2
            {
                int nextIndex = currentIndex + offset;

                if (nextIndex >= 0 && nextIndex < _nodesPerLevel)
                {
                    var candidateNode = _mapGrid[nextLevel, nextIndex];

                    if (candidateNode != null && !CanOverlapPath(currentNode, candidateNode)) // Implements Rule 3
                    {
                        potentialNextNodes.Add(candidateNode);
                    }
                }
            }

            if (potentialNextNodes.Count == 0) return null;

            // Randomly select one of the valid next nodes
            return potentialNextNodes[_pathingRNG.Next(0, potentialNextNodes.Count)];
        }

        private bool CanOverlapPath(MapNode fromNode, MapNode toNode)
        {
            // If toNode.NodeIndex == fromNode.NodeIndex, no overlap is possible (straight vertical path)
            if (toNode.Index == fromNode.Index) return false;

            // Find the path direction and the adjacent nodes in that direction
            int direction = toNode.Index > fromNode.Index ? 1 : -1;
            int fromNodeAdjacentIndex = fromNode.Index + direction;
            int toNodeAdjacentIndex = toNode.Index - direction;

            // Check bounds. If the adjacent index is out of bounds, no overlap is possible.
            if (toNodeAdjacentIndex < 0 || toNodeAdjacentIndex >= _nodesPerLevel) return false;
            if (fromNodeAdjacentIndex < 0 || fromNodeAdjacentIndex >= _nodesPerLevel) return false;

            // Check if the adjacent nodes are linked
            var fromNodeAdjacent = _mapGrid[fromNode.Level, fromNodeAdjacentIndex];
            var toNodeAdjacent = _mapGrid[toNode.Level, toNodeAdjacentIndex];

            return fromNodeAdjacent != null && toNodeAdjacent != null && toNodeAdjacent.ParentNodes.Contains(fromNodeAdjacent);
        }

        private void LinkNodes(MapNode childNode, MapNode parentNode)
        {
            if (childNode == null || parentNode == null || childNode == parentNode) return;

            if (!childNode.ParentNodes.Contains(parentNode))
                childNode.ParentNodes.Add(parentNode);

            if (!parentNode.ChildNodes.Contains(childNode))
                parentNode.ChildNodes.Add(childNode);
        }

        #endregion Pathing Logic

        #region Testing

        /// <summary>
        /// This is just for testing purposes. Call the individual methods from a control script to control the flow.
        /// </summary>
        [ContextMenu("Create Map Paths")]
        private void CreateMapPaths()
        {
            ClearPathViews();

            _maxLevels = 9;
            _nodesPerLevel = 7;
            _pathingRNG = new System.Random(0);
            _mapGrid = new MapNode[_maxLevels, _nodesPerLevel];

            // Initialize map grid with nodes
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    _mapGrid[level, nodeIndex] = new MapNode(level, nodeIndex);
                }
            }

            SelectStartingNodes();
            GeneratePaths();
            CreatePathViews();
        }

        #endregion Testing
    }
}