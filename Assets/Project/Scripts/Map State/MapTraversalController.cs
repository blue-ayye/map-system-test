using PrimeTween;
using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapTraversalController : MonoBehaviour
    {
        [Header("Traversal Settings")]
        [Tooltip("If true, allows the player to traverse back to previously visited nodes.")]
        [SerializeField] private bool _canTraverseVisitedNodes;
        [Tooltip("Maximum number of traversal steps allowed. Set to -1 (or any negative number) for unlimited steps.")]
        [SerializeField] private int _maxTraversalSteps = -1;

        [Header("Path Animation Settings")]
        [Tooltip("Duration of the path traversal animation in seconds.")]
        [SerializeField] private float _pathTraversalDuration = 0.3f;

        private List<MapNode> _visitedNodes = new List<MapNode>();
        private List<IMapPathView> _pathViews = new List<IMapPathView>();
        private int _currentTraversalSteps = 0;
        private MapNode _currentNode;
        private MapNode[,] _mapGrid;

        private MapNode _initialNode;
        private MapNode _finalNode;

        private const string _maxTraversalReachedWarning = "Maximum traversal steps reached. Cannot traverse further.";
        private const string _startLevelZeroWarning = "Please click on a valid starting node to begin.";
        private const string _invalidMoveWarning = "Invalid: You can only move to child of the current node or traverse back to visited nodes if enabled.";
        private const string _startNodeLog = "Starting at Level {0}, Index {1}";
        private const string _traverseBackLog = "Traversed back to Level {0}, Index {1}";
        private const string _movedLog = "Moved to Level {0}, Index {1}";

        public List<MapNode> VisitedNodes => _visitedNodes;
        public int TraversalStepsTaken => _currentTraversalSteps;
        public MapNode CurrentNode => _currentNode;
        public List<(MapNode From, MapNode To)> TraversedEdges { get; private set; } = new List<(MapNode, MapNode)>();

        #region Unity API

        private void OnDestroy()
        {
            ClearSubscriptions();
        }

        #endregion Unity API

        #region Public APIs

        public void ConnectMapVisuals(MapNode[,] mapGrid, List<IMapPathView> pathViews, MapNode initialNode = null, MapNode finalNode = null)
        {
            _mapGrid = mapGrid;
            _pathViews = pathViews;
            _initialNode = initialNode;
            _finalNode = finalNode;

            SubscribeToMapNodeEvents();
            UpdateAllNodeStates();
        }

        public void ClearSubscriptions()
        {
            if (_initialNode?.NodeView != null)
                _initialNode.NodeView.OnNodeClicked -= NodeView_OnNodeClicked;

            if (_finalNode?.NodeView != null)
                _finalNode.NodeView.OnNodeClicked -= NodeView_OnNodeClicked;

            if (_mapGrid == null) return;

            foreach (var node in _mapGrid)
            {
                if (node?.NodeView != null)
                {
                    node.NodeView.OnNodeClicked -= NodeView_OnNodeClicked;
                }
            }
        }

        public void ResetTraversalState()
        {
            _visitedNodes.Clear();
            TraversedEdges.Clear();
            _currentTraversalSteps = 0;
            _currentNode = null;
        }

        public Tween GetPathTraversalTween(MapNode fromNode, MapNode toNode)
        {
            foreach (IMapPathView pathView in _pathViews)
            {
                if ((pathView.FromNode == fromNode && pathView.ToNode == toNode) ||
                    (pathView.FromNode == toNode && pathView.ToNode == fromNode))
                {
                    return pathView.AnimateTraversal(_pathTraversalDuration);
                }
            }
            return default;
        }

        #endregion Public APIs

        #region Data Management

        public void WriteToMapData(MapData mapData)
        {
            mapData.MapTraversalData = new MapTraversalData(TraversedEdges, CurrentNode, TraversalStepsTaken);
        }

        public void ReadFromMapData(MapData mapData)
        {
            var traversalData = mapData.MapTraversalData;

            _visitedNodes.Clear();
            TraversedEdges.Clear();

            foreach (var pathData in traversalData.TraversedPathDataList)
            {
                var fromNode = GetNode(pathData.FromNodeData.Level, pathData.FromNodeData.Index);
                var toNode = GetNode(pathData.ToNodeData.Level, pathData.ToNodeData.Index);

                if (fromNode != null && toNode != null)
                {
                    TraversedEdges.Add((fromNode, toNode));

                    if (!_visitedNodes.Contains(fromNode)) _visitedNodes.Add(fromNode);
                    if (!_visitedNodes.Contains(toNode)) _visitedNodes.Add(toNode);
                }
            }

            if (traversalData.CurrentNodeData != null)
            {
                _currentNode = GetNode(traversalData.CurrentNodeData.Level, traversalData.CurrentNodeData.Index);

                if (_currentNode != null && !_visitedNodes.Contains(_currentNode))
                {
                    _visitedNodes.Add(_currentNode);
                }
            }
            else
            {
                _currentNode = null;
            }

            if (_currentNode != null)
            {
                _currentNode.NodeView.SetState(NodeState.Current);
            }

            _currentTraversalSteps = traversalData.TraversalStepsTaken;
            UpdateAllNodeStates();
        }

        #endregion Data Management

        #region Traversal Logic

        private void NodeView_OnNodeClicked(MapNode clickedNode)
        {
            if (_currentNode == clickedNode) return;

            if (_maxTraversalSteps >= 0 && _currentTraversalSteps >= _maxTraversalSteps)
            {
                Debug.LogWarning(_maxTraversalReachedWarning);
                return;
            }

            if (_currentNode == null)
            {
                int startLevel = _initialNode != null ? _initialNode.Level : 0;

                if (clickedNode.Level == startLevel)
                {
                    TraversePath(clickedNode);
                    Debug.LogFormat(_startNodeLog, _currentNode.Level, _currentNode.Index);
                }
                else
                {
                    Debug.LogWarning(_startLevelZeroWarning);
                }
                return;
            }

            if (_canTraverseVisitedNodes && _visitedNodes.Contains(clickedNode))
            {
                _currentTraversalSteps++;
                TraversePath(clickedNode, false);
                Debug.LogFormat(_traverseBackLog, _currentNode.Level, _currentNode.Index);
                return;
            }

            if (_currentNode.ChildNodes.Contains(clickedNode))
            {
                _currentTraversalSteps++;
                TraversePath(clickedNode);
                Debug.LogFormat(_movedLog, _currentNode.Level, _currentNode.Index);
                return;
            }

            Debug.LogWarning(_invalidMoveWarning);
        }

        private void TraversePath(MapNode clickedNode, bool animatePath = true)
        {
            MapNode previousNode = _currentNode;

            if (!_visitedNodes.Contains(clickedNode))
            {
                _visitedNodes.Add(clickedNode);
            }

            _currentNode = clickedNode;

            if (previousNode != null)
            {
                TraversedEdges.Add((previousNode, clickedNode));
                if (animatePath)
                {
                    GetPathTraversalTween(previousNode, clickedNode);
                }
            }

            RefreshChangedNodeStates(previousNode, clickedNode);
        }

        private void SubscribeToMapNodeEvents()
        {
            if (_initialNode?.NodeView != null)
                _initialNode.NodeView.OnNodeClicked += NodeView_OnNodeClicked;

            if (_finalNode?.NodeView != null)
                _finalNode.NodeView.OnNodeClicked += NodeView_OnNodeClicked;

            foreach (var node in _mapGrid)
            {
                if (node == null || node.NodeView == null) continue;

                node.NodeView.OnNodeClicked += NodeView_OnNodeClicked;
            }
        }

        #endregion Traversal Logic

        #region Node State Management

        private void UpdateAllNodeStates()
        {
            if (_initialNode != null) SetNodeState(_initialNode, GetDesiredState(_initialNode), forceUpdate: true);
            if (_finalNode != null) SetNodeState(_finalNode, GetDesiredState(_finalNode), forceUpdate: true);

            foreach (MapNode node in _mapGrid)
            {
                if (node == null)
                    continue;

                SetNodeState(node, GetDesiredState(node), forceUpdate: true);
            }
        }

        private void RefreshChangedNodeStates(MapNode previousNode, MapNode currentNode)
        {
            var affectedNodes = new HashSet<MapNode>();

            AddNodeAndChildren(affectedNodes, previousNode);
            AddNodeAndChildren(affectedNodes, currentNode);

            if (previousNode == null)
            {
                if (_initialNode != null)
                {
                    affectedNodes.Add(_initialNode);
                }
                else
                {
                    for (int index = 0; index < _mapGrid.GetLength(1); index++)
                    {
                        affectedNodes.Add(_mapGrid[0, index]);
                    }
                }
            }

            foreach (MapNode node in affectedNodes)
            {
                if (node == null)
                    continue;

                SetNodeState(node, GetDesiredState(node));
            }
        }

        private NodeState GetDesiredState(MapNode node)
        {
            if (node == _currentNode)
                return NodeState.Current;

            if (_visitedNodes.Contains(node))
                return NodeState.Visited;

            int startLevel = _initialNode != null ? _initialNode.Level : 0;
            if (_currentNode == null && node.Level == startLevel)
                return NodeState.Reachable;

            if (_currentNode != null && _currentNode.ChildNodes.Contains(node))
                return NodeState.Reachable;

            return NodeState.Locked;
        }

        private static void SetNodeState(MapNode node, NodeState newState, bool forceUpdate = false)
        {
            if (node == null || (!forceUpdate && node.State == newState))
                return;

            node.State = newState;
            node.NodeView?.SetState(newState);
        }

        private static void AddNodeAndChildren(HashSet<MapNode> nodes, MapNode node)
        {
            if (node == null)
                return;

            nodes.Add(node);

            foreach (MapNode childNode in node.ChildNodes)
            {
                nodes.Add(childNode);
            }
        }

        #endregion Node State Management

        #region Visuals

        private void AnimatePathTraversal(MapNode fromNode, MapNode toNode)
        {
            foreach (IMapPathView pathView in _pathViews)
            {
                if (pathView.FromNode == fromNode && pathView.ToNode == toNode)
                {
                    pathView.AnimateTraversal(_pathTraversalDuration);
                    return;
                }
            }
        }

        #endregion Visuals

        #region Helpers

        private MapNode GetNode(int level, int index)
        {
            if (_initialNode != null && level == _initialNode.Level) return _initialNode;
            if (_finalNode != null && level == _finalNode.Level) return _finalNode;

            if (level >= 0 && level < _mapGrid.GetLength(0) && index >= 0 && index < _mapGrid.GetLength(1))
                return _mapGrid[level, index];

            return null;
        }

        #endregion Helpers
    }
}