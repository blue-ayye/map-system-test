using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapSystemManager : MonoBehaviour
    {
        [SerializeField] private MapGridGenerator _mapGridGenerator;
        [SerializeField] private MapPathGenerator _mapPathGenerator;
        [SerializeField] private int _playerInputSeed = 0;
        [SerializeField] private bool _usePlayerInputSeed = false;

        [field: SerializeField] private int GeneratedSeed { get; set; } // Set it to private later

        [ContextMenu("Generate New Map")]
        private void Start()
        {
            _mapGridGenerator.ClearNodeViews();
            _mapPathGenerator.ClearPathViews();

            _mapGridGenerator.CreateNodeGrid();

            int? seed = _usePlayerInputSeed ? _playerInputSeed : null;
            var pRNG = InitializePRNG(seed);
            int maxLevels = _mapGridGenerator.MaxLevels;
            int nodesPerLevel = _mapGridGenerator.NodesPerLevel;
            var mapGrid = _mapGridGenerator.MapGrid;

            _mapPathGenerator.Initialize(mapGrid, maxLevels, nodesPerLevel, pRNG);
            _mapPathGenerator.SelectStartingNodes();
            _mapPathGenerator.GeneratePaths();

            _mapGridGenerator.ClearUnusedNodes();

            _mapGridGenerator.CreateNodeViews();
            _mapPathGenerator.CreatePathViews();

            AssignNodeTypes(mapGrid, pRNG);
        }

        private System.Random InitializePRNG(int? seed)
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

            return new System.Random(GeneratedSeed);
        }

        [SerializeField] private List<NodeTypeRulesSO> _nodeTypeRules;
        [SerializeField] private MapNodeTypeSO _defaultNodeType;

        private void AssignNodeTypes(MapNode[,] mapGrid, System.Random pRNG)
        {
            int maxLevels = mapGrid.GetLength(0);
            int nodesPerLevel = mapGrid.GetLength(1);

            var excludeFromOtherRulesLevels = _nodeTypeRules.Where(rules => rules.ExcludeFromOtherRules).ToList();
            foreach (var rule in excludeFromOtherRulesLevels)
            {
                for (int level = rule.StartLevel; level <= rule.EndLevel; level++)
                {
                    for (int nodeIndex = 0; nodeIndex < nodesPerLevel; nodeIndex++)
                    {
                        var node = mapGrid[level, nodeIndex];
                        if (node != null)
                        {
                            var nodeType = GetValidNodeType(node, rule, pRNG);
                            node.NodeType = nodeType;
                            var nodeView = node.NodeView;
                            if (nodeView != null)
                            {
                                nodeView.SetNodeType(nodeType);
                            }
                        }
                    }
                }
            }

            for (int level = 0; level < maxLevels; level++)
            {
                NodeTypeRulesSO nodeTypeRules = _nodeTypeRules.Find(rules => level >= rules.StartLevel && level <= rules.EndLevel);
                if (nodeTypeRules == null) continue;

                for (int nodeIndex = 0; nodeIndex < nodesPerLevel; nodeIndex++)
                {
                    var node = mapGrid[level, nodeIndex];
                    if (node != null)
                    {
                        var nodeType = GetValidNodeType(node, nodeTypeRules, pRNG);
                        node.NodeType = nodeType;
                        var nodeView = node.NodeView;
                        if (nodeView != null)
                        {
                            nodeView.SetNodeType(nodeType);
                        }
                    }
                }
            }
        }

        private MapNodeTypeSO GetValidNodeType(MapNode currentNode, NodeTypeRulesSO nodeTypeRules, System.Random pRNG)
        {
            if (nodeTypeRules.NodeTypeWeights.Count == 0)
            {
                Debug.LogWarning($"NodeTypeRulesSO '{nodeTypeRules.name}' has no NodeTypeWeights defined. Assigning default node type.");
                return _defaultNodeType;
            }

            // Make a copy of the weights to modify
            Dictionary<MapNodeTypeSO, float> availableWeights = new Dictionary<MapNodeTypeSO, float>(nodeTypeRules.NodeTypeWeights);

            // Reduce weights of consecutive node types
            var consecutiveNodes = new List<MapNode>(currentNode.ParentNodes).Concat(currentNode.ChildNodes).Where(cn => cn.NodeType != null).ToList();
            foreach (var consecutiveNode in consecutiveNodes)
            {
                if (nodeTypeRules.ConsecutiveTypeWeightReductions.TryGetValue(consecutiveNode.NodeType, out float reductionValue) && availableWeights.ContainsKey(consecutiveNode.NodeType))
                {
                    float newValue = availableWeights[consecutiveNode.NodeType] - reductionValue;
                    if (newValue <= 0f)
                    {
                        availableWeights.Remove(consecutiveNode.NodeType);
                    }
                    else
                    {
                        availableWeights[consecutiveNode.NodeType] = newValue;
                    }
                }
            }

            // Apply sibling node type constraints (Use Seed: 817167126 Grid: 9x7 Path: 3/7 to demonstrate)
            if (nodeTypeRules.SiblingTypeConstraint != SiblingNodeTypeConstraint.AllowSameType)
            {
                var allSiblings = currentNode.ParentNodes.SelectMany(p => p.ChildNodes).Where(c => c != currentNode).Distinct().ToList();
                MapNode previousSibling = null;
                MapNode nextSibling = null;
                foreach (var sibling in allSiblings)
                {
                    if (sibling.Index < currentNode.Index)
                    {
                        if (previousSibling == null || sibling.Index > previousSibling.Index)
                        {
                            previousSibling = sibling;
                        }
                    }
                    else if (sibling.Index > currentNode.Index)
                    {
                        if (nextSibling == null || sibling.Index < nextSibling.Index)
                        {
                            nextSibling = sibling;
                        }
                    }
                }

                var siblingsToCheck = nodeTypeRules.SiblingTypeConstraint == SiblingNodeTypeConstraint.DisallowSameTypeForImmediateSiblings
                    ? new List<MapNode> { previousSibling, nextSibling }
                    : allSiblings;

                foreach (var sibling in siblingsToCheck)
                {
                    if (sibling != null && sibling.NodeType != null && availableWeights.ContainsKey(sibling.NodeType))
                    {
                        availableWeights.Remove(sibling.NodeType);
                    }
                }
            }

            return GetNodeTypeByWeight(currentNode, availableWeights, pRNG);
        }

        private MapNodeTypeSO GetNodeTypeByWeight(MapNode node, Dictionary<MapNodeTypeSO, float> availableWeights, System.Random pRNG)
        {
            float totalWeight = availableWeights.Values.Sum();
            if (totalWeight <= 0f)
            {
                Debug.LogWarning($"All node types have zero weight for node at Level {node.Level}, Index {node.Index}. Assigning default node type.");
                return _defaultNodeType;
            }
            float randomValue = (float)(pRNG.NextDouble() * totalWeight);
            float cumulativeWeight = 0f;

            for (int i = 0; i < availableWeights.Count; i++)
            {
                var kvp = availableWeights.ElementAt(i);
                cumulativeWeight += kvp.Value;
                if (randomValue <= cumulativeWeight)
                {
                    return kvp.Key;
                }
            }

            Debug.LogWarning($"Failed to assign a node type for node at Level {node.Level}, Index {node.Index}. Assigning default node type.");
            return _defaultNodeType; // Fallback in case of rounding errors
        }
    }
}