using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapTraversalController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private bool _canTraverseVisitedNodes;
        [Tooltip("Maximum number of traversal steps allowed. Set to -1 (or any negative number) for unlimited steps.")]
        [SerializeField] private int _maxTraversalSteps = -1;

        private List<MapNode> _visitedNodes = new List<MapNode>();
        private int _currentTraversalSteps = 0;
        private MapNode _currentNode;
        private List<IMapPathView> _pathViews = new List<IMapPathView>();
        private MapNode[,] _mapGrid;

        #endregion

        #region Properties

        public List<MapNode> VisitedNodes => _visitedNodes;
        public int TraversalStepsTaken => _currentTraversalSteps;
        public MapNode CurrentNode => _currentNode;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            ClearSubscriptions();
        }

        #endregion

        #region Public API

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

        public void WriteTo(MapData mapData)
        {
            mapData.MapTraversalData = new MapTraversalData(VisitedNodes, CurrentNode, TraversalStepsTaken);
        }

        public void Initialize(MapNode[,] mapGrid, List<IMapPathView> pathViews)
        {
            _mapGrid = mapGrid;
            _pathViews = pathViews;
            SubscribeToMapNodeEvents();
            UpdateAllNodeStates();
            UpdatePathViewColor();
        }

        /// <summary>
        /// Restores visited-node and current-node state from <paramref name="mapData"/>.
        /// All visual state is applied in a single <see cref="UpdateAllNodeStates"/> call at
        /// the end to avoid redundant per-node view updates.
        /// </summary>
        public void ReadFrom(MapData mapData)
        {
            var traversalData = mapData.MapTraversalData;

            // Populate visited nodes based on traversal data
            _visitedNodes.Clear();

            foreach (var nodeData in traversalData.VisitedNodeDataList)
            {
                var mapNode = _mapGrid[nodeData.Level, nodeData.Index];
                if (mapNode == null)
                    continue;

                _visitedNodes.Add(mapNode);
                mapNode.State = NodeState.Visited;
            }

            // Restore current node — may be null when the player saved before selecting a start.
            // When CurrentNodeData is null we leave _currentNode null so the player picks a fresh
            // starting node; we do not silently advance them to the last visited node.
            if (traversalData.CurrentNodeData != null)
            {
                _currentNode = _mapGrid[traversalData.CurrentNodeData.Level, traversalData.CurrentNodeData.Index];
            }

            if (_currentNode != null)
            {
                _currentNode.State = NodeState.Current;
            }

            _currentTraversalSteps = traversalData.TraversalStepsTaken;

            // Apply all visual state in one pass
            UpdateAllNodeStates();
            UpdatePathViewColor();
        }

        public void ResetTraversalState()
        {
            _visitedNodes.Clear();
            _currentTraversalSteps = 0;
            _currentNode = null;
        }

        #endregion

        #region Private Helpers

        private void SubscribeToMapNodeEvents()
        {
            foreach (var node in _mapGrid)
            {
                if (node == null || node.NodeView == null) continue;

                node.NodeView.OnNodeClicked += NodeView_OnNodeClicked;
            }
        }

        private void NodeView_OnNodeClicked(MapNode clickedNode)
        {
            // Check max traversal steps
            if (_maxTraversalSteps >= 0 && _currentTraversalSteps >= _maxTraversalSteps)
            {
                Debug.LogWarning("Maximum traversal steps reached. Cannot traverse further.");
                return;
            }

            // If no current node, only allow starting at level 0 nodes
            if (_currentNode == null)
            {
                if (clickedNode.Level == 0)
                {
                    TraversePath(clickedNode);
                    Debug.Log($"Starting at Level {_currentNode.Level}, Index {_currentNode.Index}");
                }
                else
                {
                    Debug.LogWarning("Please click on a starting node at Level 0 to begin.");
                }
                return;
            }

            // Allow traversing back to visited nodes if enabled
            if (_canTraverseVisitedNodes && _visitedNodes.Contains(clickedNode))
            {
                _currentTraversalSteps++;
                TraversePath(clickedNode);
                Debug.Log($"Traversed back to Level {_currentNode.Level}, Index {_currentNode.Index}");
                return;
            }

            // Check if clicked node is a child of the current node
            if (_currentNode.ChildNodes.Contains(clickedNode))
            {
                _currentTraversalSteps++;
                TraversePath(clickedNode);
                Debug.Log($"Moved to Level {_currentNode.Level}, Index {_currentNode.Index}");
                return;
            }

            Debug.LogWarning($"Invalid: You can only move to child of the current node Node[{_currentNode.Level}, {_currentNode.Index}]" +
                $" or traverse back to visited nodes if enabled.");
        }

        private void TraversePath(MapNode clickedNode)
        {
            // Mark node as visited
            if (!_visitedNodes.Contains(clickedNode))
            {
                _visitedNodes.Add(clickedNode);
            }

            // Mark path as traversed
            IMapPathView pathView = null;
            foreach (var pv in _pathViews)
            {
                if (pv.FromNode == _currentNode && pv.ToNode == clickedNode)
                {
                    pathView = pv;
                    break;
                }
            }
            pathView?.SetTraversedColor();

            // Update current node reference
            _currentNode = clickedNode;

            UpdateAllNodeStates();
        }

        private void UpdateAllNodeStates()
        {
            foreach (var node in _mapGrid)
            {
                if (node == null) continue;

                if (_visitedNodes.Contains(node))
                {
                    node.State = node == _currentNode ? NodeState.Current : NodeState.Visited;
                }
                else if (_currentNode == null && node.Level == 0) // Starting out
                {
                    node.State = NodeState.Reachable;
                }
                else if (_currentNode != null && _currentNode.ChildNodes.Contains(node))
                {
                    node.State = NodeState.Reachable;
                }
                else
                {
                    node.State = NodeState.Locked;
                }

                // Tell the view to update its visuals
                node.NodeView.SetState(node.State);
            }
        }

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

        #endregion
    }
}
