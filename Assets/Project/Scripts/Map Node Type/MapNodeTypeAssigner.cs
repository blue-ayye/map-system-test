using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapNodeTypeAssigner : MonoBehaviour
    {
        [Header("Node Type Assignment Settings")]
        [SerializeField] private List<NodeTypeRulesSO> _nodeTypeRules;
        [SerializeField] private MapNodeTypeSO _defaultNodeType;

        private System.Random _nodeTypeRNG;
        private MapNode[,] _mapGrid;
        private int _nodesPerLevel;

        public void ReadFrom(MapData mapData)
        {
            for (int level = 0; level < _mapGrid.GetLength(0); level++)
            {
                for (int index = 0; index < _mapGrid.GetLength(1); index++)
                {
                    MapNode node = _mapGrid[level, index];
                    if (node == null)
                        continue;

                    MapNodeData loadedNodeData = mapData.MapNodeDataList.Find(n => n.Level == level && n.Index == index);
                    if (loadedNodeData == null)
                        continue;

                    var nodeType = GetNodeTypeByID(loadedNodeData.NodeTypeID);
                    if (nodeType != null)
                    {
                        node.NodeType = nodeType;
                    }
                    else
                    {
                        Debug.LogWarning($"Node type ID {loadedNodeData.NodeTypeID} not found for node at Level {level}, Index {index}.");
                    }
                }
            }
        }

        public void Initialize(MapNode[,] mapGrid, System.Random nodeTypeRNG)
        {
            _mapGrid = mapGrid;
            _nodesPerLevel = _mapGrid.GetLength(1);
            _nodeTypeRNG = nodeTypeRNG;
        }

        public void AssignNodeTypes()
        {
            // 1. First, process rules that are marked to exclude from other rules
            var staticLevels = _nodeTypeRules.Where(rules => rules.ExcludeFromOtherRules).ToList();
            SetNodeTypeByRules(staticLevels);

            // 2. Then, process the remaining rules so static levels influence them
            var normalLevels = _nodeTypeRules.Where(rules => !rules.ExcludeFromOtherRules).ToList();
            SetNodeTypeByRules(normalLevels);
        }

        public MapNodeTypeSO GetNodeTypeByID(string nodeTypeID)
        {
            foreach (var rule in _nodeTypeRules)
            {
                foreach (var nodeType in rule.NodeTypeWeights.Keys)
                {
                    if (nodeType.ID == nodeTypeID)
                    {
                        return nodeType;
                    }
                }
            }
            Debug.LogWarning($"Node type with ID '{nodeTypeID}' not found in any rules. Returning default node type.");
            return _defaultNodeType;
        }

        public int CheckTypeRulesValidity(bool logging = false)
        {
            int violations = 0;

            foreach (var rule in _nodeTypeRules)
            {
                if (rule.ExcludeFromOtherRules) continue;

                for (int level = rule.StartLevel; level <= rule.EndLevel; level++)
                {
                    for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                    {
                        var node = _mapGrid[level, nodeIndex];
                        if (node == null) continue;

                        // 1. Check consecutive node constraints
                        if (rule.ConsecutiveTypeWeightReductions.ContainsKey(node.NodeType))
                        {
                            // If the reduction is less than the weight, it means the type may still appear consecutively
                            if (rule.ConsecutiveTypeWeightReductions[node.NodeType] < rule.NodeTypeWeights[node.NodeType])
                                continue;

                            var consecutiveNodes = new List<MapNode>(node.ParentNodes).Concat(node.ChildNodes).Where(cn => cn.NodeType != null).ToList();
                            foreach (var consecutiveNode in consecutiveNodes)
                            {
                                if (consecutiveNode.NodeType == node.NodeType)
                                {
                                    violations++;
                                    if (logging)
                                        Debug.LogWarning($"Consecutive node type violation at Level {level}, Index {nodeIndex} for Node Type '{node.NodeType.DisplayName}'");
                                }
                            }
                        }

                        // 2. Check sibling node constraints
                        if (rule.SiblingTypeConstraint != SiblingNodeTypeConstraint.AllowSameType)
                        {
                            var allSiblings = node.ParentNodes.SelectMany(p => p.ChildNodes).Where(c => c != node).Distinct().ToList();
                            MapNode previousSibling = null;
                            MapNode nextSibling = null;
                            foreach (var sibling in allSiblings)
                            {
                                if (sibling.Index < node.Index)
                                {
                                    if (previousSibling == null || sibling.Index > previousSibling.Index)
                                    {
                                        previousSibling = sibling;
                                    }
                                }
                                else if (sibling.Index > node.Index)
                                {
                                    if (nextSibling == null || sibling.Index < nextSibling.Index)
                                    {
                                        nextSibling = sibling;
                                    }
                                }
                            }

                            var siblingsToCheck = rule.SiblingTypeConstraint == SiblingNodeTypeConstraint.DisallowSameTypeForImmediateSiblings
                                ? new List<MapNode> { previousSibling, nextSibling }
                                : allSiblings;

                            foreach (var sibling in siblingsToCheck)
                            {
                                if (sibling != null && sibling.NodeType == node.NodeType)
                                {
                                    violations++;
                                    if (logging)
                                        Debug.LogWarning($"Sibling node type violation at Level {level}, Index {nodeIndex} for Node Type '{node.NodeType.DisplayName}'");
                                }
                            }
                        }
                    }
                }
            }

            return violations;
        }

        private void SetNodeTypeByRules(List<NodeTypeRulesSO> nodeTypeRules)
        {
            foreach (var rule in nodeTypeRules)
            {
                for (int level = rule.StartLevel; level <= rule.EndLevel; level++)
                {
                    for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                    {
                        var node = _mapGrid[level, nodeIndex];
                        if (node == null) continue;

                        var nodeType = GetValidNodeType(node, rule);
                        node.NodeType = nodeType;
                    }
                }
            }
        }

        private MapNodeTypeSO GetValidNodeType(MapNode currentNode, NodeTypeRulesSO nodeTypeRules)
        {
            if (nodeTypeRules.NodeTypeWeights.Count == 0)
            {
                Debug.LogWarning($"NodeTypeRulesSO '{nodeTypeRules.name}' has no NodeTypeWeights defined. Assigning default node type.");
                return _defaultNodeType;
            }

            // If excluded from other rules, use weights as-is
            if (nodeTypeRules.ExcludeFromOtherRules)
            {
                return GetNodeTypeByWeight(currentNode, nodeTypeRules.NodeTypeWeights);
            }

            // Make a copy of the weights to modify
            Dictionary<MapNodeTypeSO, float> availableWeights = new Dictionary<MapNodeTypeSO, float>(nodeTypeRules.NodeTypeWeights);

            // Reduce weights of consecutive node types
            ApplyConsecutiveRules(currentNode, nodeTypeRules, availableWeights);

            // Example Seed: 1865619447/274303753/1698927251 Grid: 9x7 Path: 3/7 shows DisallowAllSiblings working well
            ApplySiblingConstraintRules(currentNode, nodeTypeRules, availableWeights);

            return GetNodeTypeByWeight(currentNode, availableWeights);
        }

        private static void ApplySiblingConstraintRules(MapNode currentNode, NodeTypeRulesSO nodeTypeRules, Dictionary<MapNodeTypeSO, float> availableWeights)
        {
            if (nodeTypeRules.SiblingTypeConstraint == SiblingNodeTypeConstraint.AllowSameType)
                return;

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

        private static void ApplyConsecutiveRules(MapNode currentNode, NodeTypeRulesSO nodeTypeRules, Dictionary<MapNodeTypeSO, float> availableWeights)
        {
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
        }

        private MapNodeTypeSO GetNodeTypeByWeight(MapNode node, Dictionary<MapNodeTypeSO, float> availableWeights)
        {
            float totalWeight = availableWeights.Values.Sum();
            if (totalWeight <= 0f)
            {
                // Debug.LogWarning($"All node types have zero weight for node at Level {node.Level}, Index {node.Index}. Assigning default node type.");
                return _defaultNodeType;
            }
            float randomValue = (float)(_nodeTypeRNG.NextDouble() * totalWeight);
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

            // Debug.LogWarning($"Failed to assign a node type for node at Level {node.Level}, Index {node.Index}. Assigning default node type.");
            return _defaultNodeType; // Fallback in case of rounding errors
        }

        [ContextMenu("Check Type Rules Validity")]
        private void ContextMenuCheckTypeRulesValidity() => CheckTypeRulesValidity(true);
    }
}