using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapPathGenerator : MonoBehaviour
    {
        [Header("Path Generation Settings")]
        [Tooltip("The number of unique starting nodes for the paths. This value must be less than or equal to the number of nodes in the first level.")]
        [SerializeField] private int _uniquePaths = 3;
        [Tooltip("The total number of paths to generate. This value must be greater than or equal to the number of unique paths.")]
        [SerializeField] private int _totalPaths = 7;

        [Header("Path View Settings")]
        [Tooltip("The parent transform under which the path view instances will be instantiated.")]
        [SerializeField] private Transform _pathViewParent;
        [Tooltip("The prefab used to instantiate path view instances.")]
        [SerializeField] private Transform _pathViewPrefab;

        private MapNode[,] _mapGrid;
        private int _maxLevels;
        private int _nodesPerLevel;
        private System.Random _pathingRNG;
        private Dictionary<int, List<MapNode>> _generatedPaths = new Dictionary<int, List<MapNode>>();

        private readonly MapNode[] _nextNodeBuffer = new MapNode[3];

        public List<IMapPathView> PathViews { get; private set; } = new List<IMapPathView>();

        #region Public APIs

        public void Initialize(MapNode[,] mapGrid, System.Random pathingRNG)
        {
            _mapGrid = mapGrid;
            _maxLevels = mapGrid.GetLength(0);
            _nodesPerLevel = mapGrid.GetLength(1);

            _pathingRNG = pathingRNG;
        }

        /// <summary>
        /// Selects starting nodes for the paths based on the specified number of unique paths and total paths.
        /// </summary>
        public void SelectStartingNodes()
        {
            int startingLevel = 0;
            _generatedPaths.Clear();

            // First, select unique starting nodes for the specified number of unique paths
            for (int pathIndex = 0; pathIndex < _uniquePaths; pathIndex++)
            {
                MapNode randomNode;
                _generatedPaths[pathIndex] = new List<MapNode>();
                do
                {
                    int randomIndex = _pathingRNG.Next(0, _nodesPerLevel);
                    randomNode = _mapGrid[startingLevel, randomIndex];
                } while (_generatedPaths[pathIndex].Contains(randomNode)); // If the node is already selected for this path, pick another

                _generatedPaths[pathIndex].Add(randomNode);
            }

            // Then, fill the remaining paths with random starting nodes, allowing for multiple paths to start from the same starting node
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

        /// <summary>
        /// Generates upward paths from the selected starting nodes to the last level of the map grid.
        /// </summary>
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

        /// <summary>
        /// Instantiates visual representations of all generated map paths.
        /// </summary>
        public void CreatePathViews()
        {
            // OPTIMIZATION: Use HashSet to prevent overlapping duplicate lines
            HashSet<(MapNode, MapNode)> drawnPaths = new HashSet<(MapNode, MapNode)>();

            foreach (List<MapNode> pathNodes in _generatedPaths.Values)
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

        /// <summary>
        /// Clears all existing path views from the parent transform and empties the tracking list.
        /// </summary>
        public void ClearPathViews()
        {
            // NOTE: If you're keeping other objects under _pathViewParent that you don't want to destroy,
            // just use GetComponentsInChildren<IMapPathView>() and destroy only those.
            foreach (Transform child in _pathViewParent)
            {
                Destroy(child.gameObject);
            }

            PathViews.Clear();
        }

        #endregion Public APIs

        #region Pathing Logic

        /// <summary>
        /// Evaluates adjacent nodes on the next level and returns a valid target that does not overlap existing paths.
        /// </summary>
        /// <param name="currentNode">The current node from which to find the next valid node.</param>
        /// <param name="nextLevel">The level of the next node to find.</param>
        /// <returns>The next valid node if found; otherwise, null.</returns>
        private MapNode GetValidNextNode(MapNode currentNode, int nextLevel)
        {
            int validCount = 0;
            int currentIndex = currentNode.Index;

            for (int offset = -1; offset <= 1; offset++) // Implements Rule 2: Check adjacent nodes (left, center, right)
            {
                int nextIndex = currentIndex + offset;

                if (nextIndex >= 0 && nextIndex < _nodesPerLevel)
                {
                    var candidateNode = _mapGrid[nextLevel, nextIndex];

                    if (candidateNode != null && !CanOverlapPath(currentNode, candidateNode)) // Implements Rule 3: Check for overlapping paths
                    {
                        _nextNodeBuffer[validCount++] = candidateNode; // Store valid candidate node
                    }
                }
            }

            if (validCount == 0) return null; // No valid next node found

            // Randomly select one of the valid next nodes
            return _nextNodeBuffer[_pathingRNG.Next(0, validCount)];
        }

        /// <summary>
        /// Evaluates if linking the two specified nodes would cause their path lines to intersect an existing path.
        /// </summary>
        /// <param name="fromNode">The starting node of the path.</param>
        /// <param name="toNode">The ending node of the path.</param>
        /// <returns>True if the path would overlap with an existing path; otherwise, false.</returns>
        private bool CanOverlapPath(MapNode fromNode, MapNode toNode)
        {
            // If toNode.NodeIndex == fromNode.NodeIndex, no overlap is possible (straight vertical path)
            if (toNode.Index == fromNode.Index) return false;

            // Determine the direction of the path (left or right) based on the indices of the nodes
            // If toNode is to the right of fromNode, direction is 1; if toNode is to the left, direction is -1
            int direction = toNode.Index > fromNode.Index ? 1 : -1;
            int fromNodeAdjacentIndex = fromNode.Index + direction;
            int toNodeAdjacentIndex = toNode.Index - direction;

            // Check if the adjacent indices are within bounds. If not, return false as there can't be an overlap
            if (toNodeAdjacentIndex < 0 || toNodeAdjacentIndex >= _nodesPerLevel || 
                fromNodeAdjacentIndex < 0 || fromNodeAdjacentIndex >= _nodesPerLevel)
                return false;

            // Check if the adjacent nodes exist and if the toNode's adjacent node has the fromNode's adjacent node as a parent,
            // which would indicate an overlap in the pathing
            var fromNodeAdjacent = _mapGrid[fromNode.Level, fromNodeAdjacentIndex];
            var toNodeAdjacent = _mapGrid[toNode.Level, toNodeAdjacentIndex];

            // If both adjacent nodes exist and the toNode's adjacent node has the fromNode's adjacent node as a parent, then there is an overlap
            return fromNodeAdjacent != null && toNodeAdjacent != null && toNodeAdjacent.ParentNodes.Contains(fromNodeAdjacent);
        }

        /// <summary>
        /// Establishes the parent-child relationship between two valid nodes.
        /// </summary>
        private void LinkNodes(MapNode childNode, MapNode parentNode)
        {
            if (childNode == parentNode) return;

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