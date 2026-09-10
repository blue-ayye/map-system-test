using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapNodeTypeAssigner : MonoBehaviour
    {
        [Header("Rules")]
        [Tooltip("Ordered rules dictating how map node types (combats, shops) are probabilistically assigned.")]
        [SerializeField] private List<NodeTypeRulesSO> _nodeTypeRules;

        [Header("Fallbacks")]
        [Tooltip("The default fallback node type used if a node matches no rules or all weighted options are exhausted.")]
        [SerializeField] private MapNodeTypeSO _defaultNodeType;

        private MapNode[,] _mapGrid;
        private System.Random _nodeTypeRNG;
        private int _nodesPerLevel;

        private const string _consecutiveViolationLog = "Consecutive node type violation at Level {0}, Index {1} for Node Type '{2}'";
        private const string _siblingViolationLog = "Sibling node type violation at Level {0}, Index {1} for Node Type '{2}'";
        private const string _noWeightsWarning = "NodeTypeRulesSO '{0}' has no NodeTypeWeights defined. Assigning default node type.";

        #region Initialization

        public void Initialize(MapNode[,] mapGrid, System.Random nodeTypeRNG)
        {
            _mapGrid = mapGrid;
            _nodesPerLevel = _mapGrid.GetLength(1);
            _nodeTypeRNG = nodeTypeRNG;
        }

        #endregion Initialization

        #region Node Type Assignment

        public void AssignNodeTypes()
        {
            // 1. Process rules marked as strict/exclusive first
            List<NodeTypeRulesSO> fixedLevels = _nodeTypeRules.Where(rules => rules.ExcludeFromOtherRules).ToList();
            SetNodeTypeByRules(fixedLevels);

            // 2. Process remaining procedural rules (fixed nodes influence these weights)
            List<NodeTypeRulesSO> proceduralLevels = _nodeTypeRules.Where(rules => !rules.ExcludeFromOtherRules).ToList();
            SetNodeTypeByRules(proceduralLevels);

            // 3. Guarantee all unassigned nodes receive a fallback type
            SetDefaultNodeTypesForUnassignedNodes();
        }

        private void SetNodeTypeByRules(List<NodeTypeRulesSO> nodeTypeRules)
        {
            foreach (var rule in nodeTypeRules)
            {
                for (int level = rule.StartLevel; level <= rule.EndLevel; level++)
                {
                    for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                    {
                        if (IsOutOfBounds(level, nodeIndex)) continue;

                        var node = _mapGrid[level, nodeIndex];
                        if (node == null) continue;

                        node.NodeType = GetValidNodeType(node, rule);
                    }
                }
            }
        }

        private void SetDefaultNodeTypesForUnassignedNodes()
        {
            for (int level = 0; level < _mapGrid.GetLength(0); level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    if (IsOutOfBounds(level, nodeIndex)) continue;

                    var node = _mapGrid[level, nodeIndex];
                    if (node == null) continue;

                    if (node.NodeType == null)
                    {
                        node.NodeType = _defaultNodeType;
                    }
                }
            }
        }

        private MapNodeTypeSO GetValidNodeType(MapNode currentNode, NodeTypeRulesSO nodeTypeRules)
        {
            if (nodeTypeRules.NodeTypeWeights.Count == 0)
            {
                Debug.LogWarningFormat(_noWeightsWarning, nodeTypeRules.name);
                return _defaultNodeType;
            }

            // If the rule is marked as exclusive, we don't need to apply any additional constraints and can directly select a node type based on the defined weights.
            if (nodeTypeRules.ExcludeFromOtherRules)
            {
                return GetNodeTypeByWeight(nodeTypeRules.NodeTypeWeights);
            }

            Dictionary<MapNodeTypeSO, float> availableWeights = new Dictionary<MapNodeTypeSO, float>(nodeTypeRules.NodeTypeWeights);

            ApplyConsecutiveRules(currentNode, nodeTypeRules, availableWeights);
            ApplySiblingConstraintRules(currentNode, nodeTypeRules, availableWeights);

            return GetNodeTypeByWeight(availableWeights);
        }

        private void ApplySiblingConstraintRules(MapNode currentNode, NodeTypeRulesSO nodeTypeRules, Dictionary<MapNodeTypeSO, float> availableWeights)
        {
            var siblingsToCheck = GetSiblingsToCheck(currentNode, nodeTypeRules.SiblingTypeConstraint);

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

        private MapNodeTypeSO GetNodeTypeByWeight(Dictionary<MapNodeTypeSO, float> availableWeights)
        {
            float totalWeight = availableWeights.Values.Sum();
            if (totalWeight <= 0f) return _defaultNodeType;

            float randomValue = (float)(_nodeTypeRNG.NextDouble() * totalWeight);
            float cumulativeWeight = 0f;

            foreach (var kvp in availableWeights)
            {
                cumulativeWeight += kvp.Value;
                if (randomValue <= cumulativeWeight)
                {
                    return kvp.Key;
                }
            }

            return _defaultNodeType;
        }

        #endregion Node Type Assignment

        #region Rule Enforcement

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
                        if (IsOutOfBounds(level, nodeIndex)) continue;

                        var node = _mapGrid[level, nodeIndex];
                        if (node == null) continue;

                        // 1. Consecutive node constraints
                        if (rule.ConsecutiveTypeWeightReductions.TryGetValue(node.NodeType, out float reductionValue))
                        {
                            if (rule.NodeTypeWeights.TryGetValue(node.NodeType, out float weightValue) && reductionValue >= weightValue)
                            {
                                var consecutiveNodes = new List<MapNode>(node.ParentNodes).Concat(node.ChildNodes).Where(cn => cn.NodeType != null).ToList();
                                foreach (var consecutiveNode in consecutiveNodes)
                                {
                                    if (consecutiveNode.NodeType == node.NodeType)
                                    {
                                        violations++;
                                        if (logging)
                                        {
                                            Debug.LogWarningFormat(_consecutiveViolationLog, level, nodeIndex, node.NodeType.DisplayName);
                                        }
                                    }
                                }
                            }
                        }

                        // 2. Sibling node constraints
                        var siblingsToCheck = GetSiblingsToCheck(node, rule.SiblingTypeConstraint);
                        foreach (var sibling in siblingsToCheck)
                        {
                            if (sibling != null && sibling.NodeType == node.NodeType)
                            {
                                violations++;
                                if (logging)
                                {
                                    Debug.LogWarningFormat(_siblingViolationLog, level, nodeIndex, node.NodeType.DisplayName);
                                }
                            }
                        }
                    }
                }
            }

            return violations;
        }

        #endregion Rule Enforcement

        #region Helpers

        private List<MapNode> GetSiblingsToCheck(MapNode currentNode, SiblingNodeTypeConstraint siblingConstraint)
        {
            if (siblingConstraint == SiblingNodeTypeConstraint.AllowSameType)
                return new List<MapNode>();

            var allSiblings = currentNode.ParentNodes.SelectMany(p => p.ChildNodes).Where(c => c != currentNode).Distinct().ToList();

            if (siblingConstraint == SiblingNodeTypeConstraint.DisallowSameTypeForAllSiblings)
                return allSiblings;

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

            var siblingsToCheck = new List<MapNode>();
            if (previousSibling != null) siblingsToCheck.Add(previousSibling);
            if (nextSibling != null) siblingsToCheck.Add(nextSibling);

            return siblingsToCheck;
        }

        private bool IsOutOfBounds(int level, int nodeIndex)
        {
            return level < 0 || level >= _mapGrid.GetLength(0) || nodeIndex < 0 || nodeIndex >= _mapGrid.GetLength(1);
        }

        #endregion Helpers

        #region Unity Editor

        [ContextMenu("Check Type Rules Validity")]
        private void ContextMenuCheckTypeRulesValidity() => CheckTypeRulesValidity(true);

        #endregion Unity Editor
    }
}