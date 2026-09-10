using PrimeTween;
using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapPathGenerator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The container transform where path view visuals will be instantiated.")]
        [SerializeField] private Transform _pathViewParent;
        [Tooltip("The prefab used to visually represent a connection between two nodes.")]
        [SerializeField] private Transform _pathViewPrefab;

        [Header("Path Generation Limits")]
        [Tooltip("The guaranteed number of unique paths that will start from the bottom level.")]
        [SerializeField] private int _uniquePaths = 3;
        [Tooltip("The total number of paths to generate. Any paths beyond the unique count may share same starting nodes.")]
        [SerializeField] private int _totalPaths = 7;

        [Header("Animation")]
        [Tooltip("How long it takes for a single path segment to animate its initial reveal.")]
        [SerializeField, Min(0.0001f)] private float _pathSpawnAnimationDuration = 0.3f;

        private MapNode[,] _mapGrid;
        private int _maxLevels;
        private int _nodesPerLevel;
        private System.Random _pathingRNG;
        private Dictionary<int, List<MapNode>> _generatedPaths = new Dictionary<int, List<MapNode>>();
        private MapNode _initialNode;
        private MapNode _finalNode;

        private readonly MapNode[] _nextNodeBuffer = new MapNode[3];

        public List<IMapPathView> PathViews { get; private set; } = new List<IMapPathView>();

        public float PathSpawnAnimationDuration { get => _pathSpawnAnimationDuration; set => _pathSpawnAnimationDuration = value; }
        public int UniquePaths { get => _uniquePaths; set => _uniquePaths = value; }
        public int TotalPaths { get => _totalPaths; set => _totalPaths = value; }

        #region Initialization

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

            // Clamp values to prevent out of bounds/infinite loops
            int safeUniquePaths = Mathf.Min(_uniquePaths, _nodesPerLevel);
            int safeTotalPaths = Mathf.Max(_totalPaths, safeUniquePaths);

            var usedStartingNodes = new HashSet<MapNode>();

            // 1. Generate unique starting nodes
            for (int pathIndex = 0; pathIndex < safeUniquePaths; pathIndex++)
            {
                MapNode randomStartingNode;
                do
                {
                    int randomIndex = _pathingRNG.Next(0, _nodesPerLevel);
                    randomStartingNode = _mapGrid[startingLevel, randomIndex];
                } while (!usedStartingNodes.Add(randomStartingNode));

                _generatedPaths[pathIndex] = new List<MapNode> { randomStartingNode };
            }

            // 2. Fill remaining path quotas (allowing shared starting nodes)
            while (_generatedPaths.Count < safeTotalPaths)
            {
                int randomIndex = _pathingRNG.Next(0, _nodesPerLevel);
                var randomStartingNode = _mapGrid[startingLevel, randomIndex];

                int pathIndex = _generatedPaths.Count;
                _generatedPaths[pathIndex] = new List<MapNode> { randomStartingNode };
            }
        }

        #endregion Initialization

        #region Path Generation

        public void GeneratePaths()
        {
            foreach (var pathEntry in _generatedPaths)
            {
                List<MapNode> pathNodes = pathEntry.Value;
                if (pathNodes == null || pathNodes.Count == 0) continue;

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

        #endregion Path Generation

        #region Visuals Management

        public void CreatePathViews()
        {
            HashSet<(MapNode, MapNode)> drawnPaths = new HashSet<(MapNode, MapNode)>();

            foreach (List<MapNode> pathNodes in _generatedPaths.Values)
            {
                for (int i = 0; i < pathNodes.Count - 1; i++)
                {
                    var fromNode = pathNodes[i];
                    var toNode = pathNodes[i + 1];

                    // Prevent drawing multiple visuals for overlapping paths
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

        #endregion Visuals Management

        #region Animation

        public void AppendLevelPathsToSequence(ref Sequence sequence, int targetLevel)
        {
            bool firstChained = false;
            foreach (var pathView in PathViews)
            {
                if (pathView.FromNode.Level == targetLevel)
                {
                    Tween pathTween = pathView.AnimateInitialDraw(_pathSpawnAnimationDuration);

                    if (!firstChained)
                    {
                        sequence.Chain(pathTween);
                        firstChained = true;
                    }
                    else
                    {
                        sequence.Group(pathTween);
                    }
                }
            }
        }

        #endregion Animation

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