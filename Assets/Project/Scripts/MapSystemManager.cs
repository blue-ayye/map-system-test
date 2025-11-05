using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapSystemManager : MonoBehaviour
    {
        [SerializeField] private MapNodeGenerator _mapGridGenerator;
        [SerializeField] private MapPathGenerator _mapPathGenerator;
        [SerializeField] private MapNodeTypeAssigner _mapNodeTypeAssigner;
        [SerializeField] private int _playerInputSeed = 0;
        [SerializeField] private bool _usePlayerInputSeed = false;
        [SerializeField] private int _generationAttempts = 1;

        private MapNode[,] _mapGrid;

        [field: SerializeField] private int GeneratedSeed { get; set; } // Set it to private later

        [ContextMenu("Generate New Map")]
        private void Start()
        {
            int attempts = 0;
            Dictionary<int, int> seedViolations = new Dictionary<int, int>();
            do
            {
                GenerateSeed();
                GenerateMap();

                int violations = _mapNodeTypeAssigner.CheckTypeRulesValidity();
                seedViolations[GeneratedSeed] = violations;

                attempts++;
            } while (attempts < _generationAttempts && seedViolations[GeneratedSeed] > 0 && !_usePlayerInputSeed);

            // If current seed has violations, pick the least violating seed from previous attempts
            if (seedViolations[GeneratedSeed] > 0)
            {
                if (!_usePlayerInputSeed)
                {
                    int bestSeed = GeneratedSeed;
                    int leastViolations = seedViolations[GeneratedSeed];
                    foreach (var kvp in seedViolations)
                    {
                        if (kvp.Value < leastViolations)
                        {
                            leastViolations = kvp.Value;
                            bestSeed = kvp.Key;
                        }
                    }

                    GeneratedSeed = bestSeed;

                    // Re-generate the map with the best seed
                    GenerateMap();
                    _mapNodeTypeAssigner.CheckTypeRulesValidity(true);

                    Debug.LogWarning($"Could not generate a valid map within {_generationAttempts} attempts. " +
                                     $"Using seed {GeneratedSeed} with {leastViolations} rule violations.");
                }
                else
                {
                    _mapNodeTypeAssigner.CheckTypeRulesValidity(true);
                    Debug.LogWarning($"You're using a custom seed {_playerInputSeed} that resulted in " +
                                     $"{seedViolations[GeneratedSeed]} rule violations. " +
                                     $"Consider using a different seed or toggle off the custom seed option" +
                                     $" to allow automatic seed generation with least violations.");
                }
            }

            GenerateMapVisuals();
            SubscribeToMapNodeEvents();
        }

        private void OnDestroy()
        {
            foreach (var node in _mapGrid)
            {
                if (node == null || node.NodeView == null) continue;

                node.NodeView.OnNodeClicked -= NodeView_OnNodeClicked;
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

        private void GenerateSeed()
        {
            if (_usePlayerInputSeed)
            {
                GeneratedSeed = Mathf.Abs(_playerInputSeed);
            }
            else
            {
                int intMinMaxSeed = Random.Range(int.MinValue, int.MaxValue);
                int dateTimeSeed = System.DateTime.Now.Millisecond;
                GeneratedSeed = Mathf.Abs(intMinMaxSeed + dateTimeSeed);
            }
        }

        private void GenerateMap()
        {
            // 1. Create map node data
            var mapJitterRNG = new System.Random(GeneratedSeed);
            _mapGridGenerator.Initialize(mapJitterRNG);
            _mapGrid = _mapGridGenerator.CreateNodeGrid();

            // 2. Create map path data
            var mapPathingRNG = new System.Random(GeneratedSeed + 1);
            _mapPathGenerator.Initialize(_mapGrid, mapPathingRNG);
            _mapPathGenerator.SelectStartingNodes();
            _mapPathGenerator.GeneratePaths();
            _mapGridGenerator.ClearUnusedNodes();

            // 3. Assign node types to map node data
            var mapNodeTypeRNG = new System.Random(GeneratedSeed + 2);
            _mapNodeTypeAssigner.Initialize(_mapGrid, mapNodeTypeRNG);
            _mapNodeTypeAssigner.AssignNodeTypes();
        }

        private void GenerateMapVisuals()
        {
            _mapGridGenerator.ClearNodeViews();
            _mapPathGenerator.ClearPathViews();
            _currentNode = null;
            _visitedNodes.Clear();

            _mapGridGenerator.CreateNodeViews();
            _mapPathGenerator.CreatePathViews();
        }

        private List<MapNode> _visitedNodes = new List<MapNode>();
        private MapNode _currentNode;
        [SerializeField] private bool _canTraverseVisitedNodes;

        private void NodeView_OnNodeClicked(MapNode clickedNode)
        {
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
                TraversePath(clickedNode);
                Debug.Log($"Traversed back to Level {_currentNode.Level}, Index {_currentNode.Index}");
                return;
            }

            // Check if clicked node is a child of the current node
            if (_currentNode.ChildNodes.Contains(clickedNode))
            {
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
            foreach (var pv in _mapPathGenerator.PathViews)
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