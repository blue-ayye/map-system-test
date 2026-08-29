using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BP.MapSystem
{
    /// <summary>
    /// This class is responsible for assigning node types to a grid of MapNodes based on defined rules.
    /// It ensures that the assigned node types adhere to constraints such as consecutive node type restrictions and sibling node type constraints.
    /// The assignment process is influenced by weighted probabilities defined in NodeTypeRulesSO, allowing for flexible and dynamic map generation.
    /// </summary>
    public class MapNodeTypeAssigner : MonoBehaviour
    {
        [Tooltip("List of NodeTypeRulesSO that define the rules for assigning node types to the map grid.")]
        [SerializeField] private List<NodeTypeRulesSO> _nodeTypeRules;
        [Tooltip("Default node type to assign when no valid node type can be determined based on the rules.")]
        [SerializeField] private MapNodeTypeSO _defaultNodeType;

        private MapNode[,] _mapGrid;
        private System.Random _nodeTypeRNG;
        private int _nodesPerLevel;

        private const string _consecutiveViolationLog = "Consecutive node type violation at Level {0}, Index {1} for Node Type '{2}'";
        private const string _siblingViolationLog = "Sibling node type violation at Level {0}, Index {1} for Node Type '{2}'";
        private const string _noWeightsWarning = "NodeTypeRulesSO '{0}' has no NodeTypeWeights defined. Assigning default node type.";

        #region Public APIs

        public void Initialize(MapNode[,] mapGrid, System.Random nodeTypeRNG)
        {
            _mapGrid = mapGrid;
            _nodesPerLevel = _mapGrid.GetLength(1);

            _nodeTypeRNG = nodeTypeRNG;
        }

        public void AssignNodeTypes()
        {
            // 1. First, process rules that are marked to exclude from other rules
            List<NodeTypeRulesSO> fixedLevels = _nodeTypeRules.Where(rules => rules.ExcludeFromOtherRules).ToList();
            SetNodeTypeByRules(fixedLevels);

            // 2. Then, process the remaining rules so fixed levels influence them
            List<NodeTypeRulesSO> proceduralLevels = _nodeTypeRules.Where(rules => !rules.ExcludeFromOtherRules).ToList();
            SetNodeTypeByRules(proceduralLevels);
        }

        #endregion Public APIs

        #region Node Assignment Logic

        /// <summary>
        /// Assigns node types to the map grid based on the provided list of NodeTypeRulesSO.
        /// </summary>
        /// <param name="nodeTypeRules">The list of NodeTypeRulesSO to use for assigning node types.</param>
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

                        node.NodeType = GetValidNodeType(node, rule);
                    }
                }
            }
        }

        /// <summary>
        /// Determines a valid node type for the given MapNode based on the specified NodeTypeRulesSO.
        /// </summary>
        /// <param name="currentNode">The MapNode for which to determine a valid node type.</param>
        /// <param name="nodeTypeRules">The NodeTypeRulesSO to use for determining a valid node type.</param>
        /// <returns>The determined MapNodeTypeSO.</returns>
        private MapNodeTypeSO GetValidNodeType(MapNode currentNode, NodeTypeRulesSO nodeTypeRules)
        {
            if (nodeTypeRules.NodeTypeWeights.Count == 0)
            {
                Debug.LogWarningFormat(_noWeightsWarning, nodeTypeRules.name);
                return _defaultNodeType;
            }

            // If excluded from other rules, use weights as-is
            if (nodeTypeRules.ExcludeFromOtherRules)
            {
                return GetNodeTypeByWeight(nodeTypeRules.NodeTypeWeights);
            }

            // Make a copy of the weights to modify
            Dictionary<MapNodeTypeSO, float> availableWeights = new Dictionary<MapNodeTypeSO, float>(nodeTypeRules.NodeTypeWeights);

            // Reduce weights of consecutive node types
            ApplyConsecutiveRules(currentNode, nodeTypeRules, availableWeights);

            // Example Seed: 1865619447/274303753/1698927251 Grid: 9x7 Path: 3/7 shows DisallowAllSiblings working well
            ApplySiblingConstraintRules(currentNode, nodeTypeRules, availableWeights);

            return GetNodeTypeByWeight(availableWeights);
        }

        /// <summary>
        /// Applies sibling node type constraints to the available weights for the current node.
        /// Removes the weights of sibling node types based on the defined sibling constraints (AllowSameType, DisallowSameTypeForImmediateSiblings, DisallowSameTypeForAllSiblings) in the NodeTypeRulesSO.
        /// </summary>
        /// <param name="currentNode">The MapNode for which to apply sibling constraints.</param>
        /// <param name="nodeTypeRules">The NodeTypeRulesSO containing the sibling constraints.</param>
        /// <param name="availableWeights">The dictionary of available weights to modify based on sibling constraints.</param>
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

        /// <summary>
        /// Applies consecutive node type weight reductions to the available weights for the current node.
        /// Basically, in the ConsecutiveTypeWeightReductions, we can set a weight reduction amount for consecutive node types instead of outright removing them.
        /// This allows for more nuanced control over the probability of consecutive node types appearing in the map grid.
        /// </summary>
        /// <param name="currentNode">The MapNode for which to apply consecutive weight reductions.</param>
        /// <param name="nodeTypeRules">The NodeTypeRulesSO containing the consecutive weight reduction rules.</param>
        /// <param name="availableWeights">The dictionary of available weights to modify based on consecutive weight reductions.</param>
        private static void ApplyConsecutiveRules(MapNode currentNode, NodeTypeRulesSO nodeTypeRules, Dictionary<MapNodeTypeSO, float> availableWeights)
        {
            // Get all consecutive nodes (parents and children) of the current node
            var consecutiveNodes = new List<MapNode>(currentNode.ParentNodes).Concat(currentNode.ChildNodes).Where(cn => cn.NodeType != null).ToList();

            // Reduce the weights of consecutive node types based on the defined consecutive weight reduction rules
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

        /// <summary>
        /// Selects a MapNodeTypeSO from the available weights using a weighted random selection.
        /// </summary>
        /// <param name="availableWeights">The dictionary of available weights to select from.</param>
        /// <returns>The selected MapNodeTypeSO based on the weighted random selection.</returns>
        private MapNodeTypeSO GetNodeTypeByWeight(Dictionary<MapNodeTypeSO, float> availableWeights)
        {
            float totalWeight = availableWeights.Values.Sum();
            if (totalWeight <= 0f)
            {
                return _defaultNodeType;
            }

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

            return _defaultNodeType; // Fallback in case of rounding errors
        }

        #endregion Node Assignment Logic

        #region Rule Enforcement

        public int CheckTypeRulesValidity(bool logging = false)
        {
            int violations = 0;

            foreach (var rule in _nodeTypeRules)
            {
                // Skip rules that are marked to exclude from other rules, as they are not subject to validation against other rules
                if (rule.ExcludeFromOtherRules) continue;

                for (int level = rule.StartLevel; level <= rule.EndLevel; level++)
                {
                    for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                    {
                        var node = _mapGrid[level, nodeIndex];
                        if (node == null) continue;

                        // 1. Check consecutive node constraints
                        if (rule.ConsecutiveTypeWeightReductions.TryGetValue(node.NodeType, out float reductionValue))
                        {
                            // Guard against missing keys in the weights dictionary
                            if (rule.NodeTypeWeights.TryGetValue(node.NodeType, out float weightValue) && reductionValue >= weightValue)
                            {
                                var consecutiveNodes = new List<MapNode>(node.ParentNodes).Concat(node.ChildNodes).Where(cn => cn.NodeType != null).ToList();
                                foreach (var consecutiveNode in consecutiveNodes)
                                {
                                    if (consecutiveNode.NodeType == node.NodeType)
                                    {
                                        violations++;
                                        if (logging)
                                            Debug.LogWarningFormat(_consecutiveViolationLog, level, nodeIndex, node.NodeType.DisplayName);
                                    }
                                }
                            }
                        }

                        // 2. Check sibling node constraints
                        var siblingsToCheck = GetSiblingsToCheck(node, rule.SiblingTypeConstraint);
                        foreach (var sibling in siblingsToCheck)
                        {
                            if (sibling != null && sibling.NodeType == node.NodeType)
                            {
                                violations++;
                                if (logging)
                                    Debug.LogWarningFormat(_siblingViolationLog, level, nodeIndex, node.NodeType.DisplayName);
                            }
                        }
                    }
                }
            }

            return violations;
        }

        #endregion Rule Enforcement

        #region Helper Methods

        /// <summary>
        /// Gets the list of sibling nodes to check based on the specified sibling constraint.
        /// </summary>
        /// <param name="currentNode">The current map node.</param>
        /// <param name="siblingConstraint">The sibling node type constraint.</param>
        /// <returns>The list of sibling nodes to check if they have same type as the current node.</returns>
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

        #endregion Helper Methods

        #region Unity Editor

        [ContextMenu("Check Type Rules Validity")]
        private void ContextMenuCheckTypeRulesValidity() => CheckTypeRulesValidity(true);

        #endregion Unity Editor
    }
}