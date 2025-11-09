using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapTraversalController : MonoBehaviour
    {
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

        private void OnDestroy()
        {
            foreach (var node in _mapGrid)
            {
                if (node == null || node.NodeView == null) continue;

                node.NodeView.OnNodeClicked -= NodeView_OnNodeClicked;
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
        }

        public void ReadFrom(MapData mapData)
        {
            var traversalData = mapData.MapTraversalData;

            // Populate visited nodes based on traversal data
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
                node.NodeView.SetActiveVisitedState(true);
            }

            foreach (var pathView in _pathViews)
            {
                if (_visitedNodes.Contains(pathView.FromNode) && _visitedNodes.Contains(pathView.ToNode))
                {
                    pathView.ChangePathColor(Color.yellow); // Indicate traversed paths
                }
            }

            // Set current node
            _currentNode = _mapGrid[traversalData.CurrentNodeData.Level, traversalData.CurrentNodeData.Index];

            if (_currentNode == null && _visitedNodes.Count == 0)
            {
                _currentNode = _visitedNodes[^1]; // Last visited node just in case of data mismatch
            }

            if (_currentNode != null)
            {
                _currentNode.NodeView.SetActiveSelectedState(true);
            }

            _currentTraversalSteps = traversalData.TraversalStepsTaken;
        }

        public void ResetTraversalState()
        {
            _visitedNodes.Clear();
            _currentTraversalSteps = 0;
            _currentNode = null;
        }

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
                clickedNode.NodeView.SetActiveVisitedState(true);
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
            pathView?.ChangePathColor(Color.yellow); // Change color to indicate traversal

            // Update current node selection state
            _currentNode?.NodeView.SetActiveSelectedState(false);
            clickedNode.NodeView.SetActiveSelectedState(true);

            // Update current node reference
            _currentNode = clickedNode;
        }
    }
}