using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapTraversalController : MonoBehaviour
    {
        private const string _maxTraversalReachedWarning = "Maximum traversal steps reached. Cannot traverse further.";
        private const string _startLevelZeroWarning = "Please click on a starting node at Level 0 to begin.";
        private const string _invalidMoveWarning = "Invalid: You can only move to child of the current node or traverse back to visited nodes if enabled.";
        private const string _startNodeLog = "Starting at Level {0}, Index {1}";
        private const string _traverseBackLog = "Traversed back to Level {0}, Index {1}";
        private const string _movedLog = "Moved to Level {0}, Index {1}";

        [SerializeField] private bool _canTraverseVisitedNodes;
        [Tooltip("Maximum number of traversal steps allowed. Set to -1 (or any negative number) for unlimited steps.")]
        [SerializeField] private int _maxTraversalSteps = -1;

        private List<MapNode> _visitedNodes = new List<MapNode>();
        private int _currentTraversalSteps = 0;
        private MapNode _currentNode;
        private List<IMapPathView> _pathViews = new List<IMapPathView>();
        private MapNode[,] _mapGrid;

        public List<MapNode> VisitedNodes => _visitedNodes;
        public int TraversalStepsTaken => _currentTraversalSteps;
        public MapNode CurrentNode => _currentNode;

        #region Unity API

        private void OnDestroy()
        {
            ClearSubscriptions();
        }

        #endregion Unity API

        #region Public APIs

        public void ConnectMapVisuals(MapNode[,] mapGrid, List<IMapPathView> pathViews)
        {
            _mapGrid = mapGrid;
            _pathViews = pathViews;
            SubscribeToMapNodeEvents();
            UpdateAllNodeStates();
            UpdatePathViewColor();
        }

        public void ClearSubscriptions()
        {
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
            _currentTraversalSteps = 0;
            _currentNode = null;
        }

        #endregion Public APIs

        #region Data Management

        public void WriteTo(MapData mapData)
        {
            mapData.MapTraversalData = new MapTraversalData(VisitedNodes, CurrentNode, TraversalStepsTaken);
        }

        public void ReadFrom(MapData mapData)
        {
            var traversalData = mapData.MapTraversalData;

            _visitedNodes.Clear();

            foreach (var node in traversalData.VisitedNodeDataList)
            {
                var mapNode = _mapGrid[node.Level, node.Index];
                if (mapNode == null)
                    continue;

                _visitedNodes.Add(mapNode);
            }

            foreach (var node in _visitedNodes)
            {
                node.NodeView.SetState(NodeState.Visited);
            }

            _currentNode = _mapGrid[traversalData.CurrentNodeData.Level, traversalData.CurrentNodeData.Index];

            if (_currentNode == null && _visitedNodes.Count > 0)
            {
                _currentNode = _visitedNodes[^1];
            }

            if (_currentNode != null)
            {
                _currentNode.NodeView.SetState(NodeState.Current);
            }

            _currentTraversalSteps = traversalData.TraversalStepsTaken;
            UpdateAllNodeStates();
            UpdatePathViewColor();
        }

        #endregion Data Management

        #region Traversal Logic

        private void NodeView_OnNodeClicked(MapNode clickedNode)
        {
            if (_maxTraversalSteps >= 0 && _currentTraversalSteps >= _maxTraversalSteps)
            {
                Debug.LogWarning(_maxTraversalReachedWarning);
                return;
            }

            if (_currentNode == null)
            {
                if (clickedNode.Level == 0)
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
                TraversePath(clickedNode);
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

        private void TraversePath(MapNode clickedNode)
        {
            MapNode previousNode = _currentNode;

            if (!_visitedNodes.Contains(clickedNode))
            {
                _visitedNodes.Add(clickedNode);
            }

            _currentNode = clickedNode;

            if (previousNode != null)
            {
                SetPathAsTraversed(previousNode, clickedNode);
            }

            RefreshChangedNodeStates(previousNode, clickedNode);
        }

        #endregion Traversal Logic

        #region Node State Management

        private void UpdateAllNodeStates()
        {
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
                for (int index = 0; index < _mapGrid.GetLength(1); index++)
                {
                    affectedNodes.Add(_mapGrid[0, index]);
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

            if (_currentNode == null && node.Level == 0)
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

        #region Path Visuals & Events

        private void UpdatePathViewColor()
        {
            foreach (var pathView in _pathViews)
            {
                if (_visitedNodes.Contains(pathView.FromNode) && _visitedNodes.Contains(pathView.ToNode))
                {
                    pathView.SetTraversedColor();
                }
                else
                {
                    pathView.SetDefaultColor();
                }
            }
        }

        private void SetPathAsTraversed(MapNode fromNode, MapNode toNode)
        {
            foreach (IMapPathView pathView in _pathViews)
            {
                if (pathView.FromNode == fromNode && pathView.ToNode == toNode)
                {
                    pathView.SetTraversedColor();
                    return;
                }
            }
        }

        private void SubscribeToMapNodeEvents()
        {
            foreach (var node in _mapGrid)
            {
                if (node == null || node.NodeView == null) continue;

                node.NodeView.OnNodeClicked += NodeView_OnNodeClicked;
            }
        }

        #endregion Path Visuals & Events
    }
}