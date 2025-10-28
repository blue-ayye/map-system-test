using System.Collections.Generic;
using UnityEngine;

namespace BP.MapGeneration
{
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

    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private MapGridGenerator _mapGridGenerator;
        [SerializeField] private int _maxLevels = 9;
        [SerializeField] private int _nodesPerLevel = 7;
        [SerializeField] private int _playerInputSeed = 0;
        [SerializeField] private bool _usePlayerInputSeed = false;
        [SerializeField] private int _uniquePaths = 3;
        [SerializeField] private int _totalPaths = 7;
        [SerializeField] private Transform _nodeViewParent;
        [SerializeField] private Transform _nodeViewPrefab;
        [SerializeField] private Transform _pathViewParent;
        [SerializeField] private Transform _pathViewPrefab;

        private MapNode[,] _mapGrid;
        private System.Random _pRNG;
        public int GeneratedSeed { get; private set; }
        private List<MapNode> _startingNodes = new List<MapNode>();

        private void OnDrawGizmos()
        {
            if (_mapGrid == null)
                return;

            // Draw nodes
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    Gizmos.color = _startingNodes.Contains(_mapGrid[level, nodeIndex]) ? Color.green : Color.white;
                    Vector3 position = new Vector3(nodeIndex * 2.0f, level * 2.0f, 0);
                    Gizmos.DrawSphere(position, 0.3f);
                }
            }

            // Draw connections
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = _mapGrid[level, nodeIndex];
                    if (node == null) continue;
                    Vector3 fromPosition = new Vector3(nodeIndex * 2.0f, level * 2.0f, 0);
                    foreach (var child in node.ChildNodes)
                    {
                        Vector3 toPosition = new Vector3(child.NodeIndex * 2.0f, child.Level * 2.0f, 0);
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(fromPosition, toPosition);
                    }
                }
            }
        }

        [ContextMenu("Generate New Map")]
        private void Start()
        {
            _mapGridGenerator.ClearNodeViews();
            ClearPathViews();

            _mapGridGenerator.CreateNodeGrid();

            _mapGrid = _mapGridGenerator.MapGrid;


            int? seed = _usePlayerInputSeed ? _playerInputSeed : (int?)null;
            InitializePRNG(seed);
            SelectStartingNodes();
            GeneratePaths();

            _mapGridGenerator.ClearUnusedNodes();

            _mapGridGenerator.CreateNodeViews();
            CreatePathViews();
        }



        // Path management

        private void ClearPathViews()
        {
            foreach (Transform child in _pathViewParent)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreatePathViews()
        {
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = _mapGrid[level, nodeIndex];
                    if (node == null) continue;
                    Vector3 fromPosition = new Vector3(nodeIndex * 2.0f, level * 2.0f, 0);
                    foreach (var child in node.ChildNodes)
                    {
                        Vector3 toPosition = new Vector3(child.NodeIndex * 2.0f, child.Level * 2.0f, 0);
                        var pathView = Instantiate(_pathViewPrefab, _pathViewParent);
                        var lineRenderer = pathView.GetComponent<LineRenderer>();
                        if (lineRenderer != null)
                        {
                            lineRenderer.positionCount = 2;
                            lineRenderer.SetPosition(0, fromPosition);
                            lineRenderer.SetPosition(1, toPosition);
                        }
                    }
                }
            }
        }

        private void SelectStartingNodes()
        {
            int startingLevel = 0;
            _startingNodes.Clear();
            // Ensure at least <MinUniqueStartingPoints> number of nodes are unique
            for (int i = 0; i < _uniquePaths; i++)
            {
                MapNode randomNode;
                do
                {
                    int randomIndex = _pRNG.Next(0, _nodesPerLevel);
                    randomNode = _mapGrid[startingLevel, randomIndex];
                } while (_startingNodes.Contains(randomNode));
                _startingNodes.Add(randomNode);
            }

            // Then fill the rest allowing duplicate nodes to allow multiple paths from same starting point
            while (_startingNodes.Count < _totalPaths)
            {
                int randomIndex = _pRNG.Next(0, _nodesPerLevel);
                var randomNode = _mapGrid[startingLevel, randomIndex];
                _startingNodes.Add(randomNode);
            }
        }

        public void GeneratePaths()
        {
            for (int pathIndex = 0; pathIndex < _startingNodes.Count; pathIndex++)
            {
                Color debugPathColor = Color.HSVToRGB((float)pathIndex / _startingNodes.Count, 1f, 1f);
                MapNode currentNode = _startingNodes[pathIndex];
                for (int level = 0; level < _maxLevels - 1; level++)
                {
                    var nextNode = GetValidNextNode(currentNode, level + 1);
                    if (nextNode == null) break; // No valid next node, end this path

                    LinkNodes(nextNode, currentNode);
                    currentNode = nextNode;
                }
            }
        }

        private MapNode GetValidNextNode(MapNode currentNode, int nextLevel)
        {
            var nextLevelNodes = GetNodesAt(nextLevel);
            if (nextLevelNodes.Count == 0) return null;

            List<MapNode> potentialNextNodes = new List<MapNode>();
            int currentIndex = currentNode.NodeIndex;
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
            var selectedNode = potentialNextNodes[_pRNG.Next(0, potentialNextNodes.Count)];
            return selectedNode;
        }

        private bool CanOverlapPath(MapNode fromNode, MapNode toNode)
        {
            // If toNode.NodeIndex == fromNode.NodeIndex, no overlap is possible
            if (toNode.NodeIndex == fromNode.NodeIndex) return false;

            // Find the path direction and the adjacent nodes in that direction
            int direction = toNode.NodeIndex > fromNode.NodeIndex ? 1 : -1;
            int fromNodeAdjacentIndex = fromNode.NodeIndex + direction;
            int toNodeAdjacentIndex = toNode.NodeIndex - direction;

            // Check bounds. If the adjacent index is out of bounds, no overlap is possible.
            if (toNodeAdjacentIndex < 0 || toNodeAdjacentIndex >= _nodesPerLevel) return false;
            if (fromNodeAdjacentIndex < 0 || fromNodeAdjacentIndex >= _nodesPerLevel) return false;

            // Check if the adjacent nodes are linked
            var fromNodeAdjacent = _mapGrid[fromNode.Level, fromNodeAdjacentIndex];
            var toNodeAdjacent = _mapGrid[toNode.Level, toNodeAdjacentIndex];
            return fromNodeAdjacent != null && toNodeAdjacent != null && toNodeAdjacent.ParentNodes.Contains(fromNodeAdjacent);
        }

        private void LinkNodes(MapNode childNode, MapNode parentNode, Color? pathColor = null)
        {
            if (childNode == null || parentNode == null || childNode == parentNode) return;

            if (!childNode.ParentNodes.Contains(parentNode))
                childNode.ParentNodes.Add(parentNode);
            if (!parentNode.ChildNodes.Contains(childNode))
                parentNode.ChildNodes.Add(childNode);
        }

        private void InitializePRNG(int? seed)
        {
            if (seed == null)
            {
                int intMinMaxSeed = Random.Range(int.MinValue, int.MaxValue);
                int dateTimeSeed = System.DateTime.Now.Millisecond;
                GeneratedSeed = Mathf.Abs(intMinMaxSeed + dateTimeSeed);
            }
            else
            {
                GeneratedSeed = Mathf.Abs(seed.Value);
            }

            _pRNG = new System.Random(GeneratedSeed);
        }

        private List<MapNode> GetNodesAt(int level)
        {
            List<MapNode> nodes = new List<MapNode>();
            if (_mapGrid == null || level < 0 || level >= _maxLevels) return nodes;

            for (int i = 0; i < _nodesPerLevel; i++)
            {
                var node = _mapGrid[level, i];
                if (node != null) nodes.Add(node);
            }

            return nodes;
        }
    }
}