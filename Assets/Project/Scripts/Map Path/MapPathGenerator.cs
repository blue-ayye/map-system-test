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
        private MapNode _initialNode;
        private MapNode _finalNode;

        private readonly MapNode[] _nextNodeBuffer = new MapNode[3];

        public List<IMapPathView> PathViews { get; private set; } = new List<IMapPathView>();

        #region Public APIs

        public void Initialize(MapNode[,] mapGrid, System.Random pathingRNG, MapNode initialNode = null, MapNode finalNode = null)
        {
            _mapGrid = mapGrid;
            _maxLevels = mapGrid.GetLength(0);
            _nodesPerLevel = mapGrid.GetLength(1);

            _pathingRNG = pathingRNG;
            _initialNode = initialNode;
            _finalNode = finalNode;
        }

        public void SelectStartingNodes()
        {
            int startingLevel = 0;
            _generatedPaths.Clear();

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

                if (_initialNode != null)
                {
                    LinkNodes(currentNode, _initialNode);
                    pathNodes.Insert(0, _initialNode);
                }

                for (int level = 0; level < _maxLevels - 1; level++)
                {
                    var nextNode = GetValidNextNode(currentNode, level + 1);
                    if (nextNode == null) break;

                    LinkNodes(nextNode, currentNode);

                    currentNode = nextNode;
                    pathNodes.Add(currentNode);
                }

                if (_finalNode != null && currentNode != null)
                {
                    LinkNodes(_finalNode, currentNode);
                    pathNodes.Add(_finalNode);
                }
            }
        }

        public void CreatePathViews()
        {
            HashSet<(MapNode, MapNode)> drawnPaths = new HashSet<(MapNode, MapNode)>();

            foreach (List<MapNode> pathNodes in _generatedPaths.Values)
            {
                for (int i = 0; i < pathNodes.Count - 1; i++)
                {
                    var fromNode = pathNodes[i];
                    var toNode = pathNodes[i + 1];

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
            int validCount = 0;
            int currentIndex = currentNode.Index;

            for (int offset = -1; offset <= 1; offset++)
            {
                int nextIndex = currentIndex + offset;

                if (nextIndex >= 0 && nextIndex < _nodesPerLevel)
                {
                    var candidateNode = _mapGrid[nextLevel, nextIndex];

                    if (candidateNode != null && !CanOverlapPath(currentNode, candidateNode))
                    {
                        _nextNodeBuffer[validCount++] = candidateNode;
                    }
                }
            }

            if (validCount == 0) return null;

            return _nextNodeBuffer[_pathingRNG.Next(0, validCount)];
        }

        private bool CanOverlapPath(MapNode fromNode, MapNode toNode)
        {
            if (toNode.Index == fromNode.Index) return false;

            int direction = toNode.Index > fromNode.Index ? 1 : -1;
            int fromNodeAdjacentIndex = fromNode.Index + direction;
            int toNodeAdjacentIndex = toNode.Index - direction;

            if (toNodeAdjacentIndex < 0 || toNodeAdjacentIndex >= _nodesPerLevel ||
                fromNodeAdjacentIndex < 0 || fromNodeAdjacentIndex >= _nodesPerLevel)
                return false;

            var fromNodeAdjacent = _mapGrid[fromNode.Level, fromNodeAdjacentIndex];
            var toNodeAdjacent = _mapGrid[toNode.Level, toNodeAdjacentIndex];

            return fromNodeAdjacent != null && toNodeAdjacent != null && toNodeAdjacent.ParentNodes.Contains(fromNodeAdjacent);
        }

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

        [ContextMenu("Create Map Paths")]
        private void CreateMapPaths()
        {
            ClearPathViews();

            _maxLevels = 9;
            _nodesPerLevel = 7;
            _pathingRNG = new System.Random(0);
            _mapGrid = new MapNode[_maxLevels, _nodesPerLevel];

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