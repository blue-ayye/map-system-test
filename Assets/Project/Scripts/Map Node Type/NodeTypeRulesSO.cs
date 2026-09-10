using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public enum SiblingNodeTypeConstraint
    {
        AllowSameType,
        DisallowSameTypeForImmediateSiblings,
        DisallowSameTypeForAllSiblings
    }

    [CreateAssetMenu(fileName = "NodeTypeRules", menuName = "Map System/Node Type Rules")]
    public class NodeTypeRulesSO : ScriptableObject
    {
        [Header("Level Range")]
        [Tooltip("Start level index (inclusive) for this rule set.")]
        [SerializeField] private int _startLevel;
        [Tooltip("End level index (inclusive) for this rule set.")]
        [SerializeField] private int _endLevel;

        [Header("Constraints")]
        [Tooltip("If true, these levels are evaluated independently and will not be influenced by adjacent rules.")]
        [SerializeField] private bool _excludeFromOtherRules;
        [Tooltip("Rule constraint for handling identical node types across sibling branches.")]
        [SerializeField] private SiblingNodeTypeConstraint _siblingNodeTypeConstraint;

        [Header("Weights")]
        [Tooltip("Base weighted probabilities for each node type in this level range.")]
        [SerializedDictionary("Node Type", "Weight")]
        [SerializeField] private SerializedDictionary<MapNodeTypeSO, float> _nodeTypeWeights;
        [Tooltip("Weight reduction applied if a connected parent or child shares this node type.")]
        [SerializedDictionary("Node Type", "Weight Reduction")]
        [SerializeField] private SerializedDictionary<MapNodeTypeSO, float> _consecutiveTypeWeightReductions;

        public int StartLevel => _startLevel;
        public int EndLevel => _endLevel;
        public bool ExcludeFromOtherRules => _excludeFromOtherRules;
        public Dictionary<MapNodeTypeSO, float> NodeTypeWeights => _nodeTypeWeights;
        public Dictionary<MapNodeTypeSO, float> ConsecutiveTypeWeightReductions => _consecutiveTypeWeightReductions;
        public SiblingNodeTypeConstraint SiblingTypeConstraint => _siblingNodeTypeConstraint;

        #region Unity Editor

#if UNITY_EDITOR

        private const string _fileNameSuffix = "_NodeTypeRules";
        private const string _duplicateAssetWarning = "An asset with the name '{0}' already exists. Cannot rename.";

        [ContextMenu("Rename File to Match Display Name")]
        public void RenameFile()
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string newFileName = $"{_startLevel}-{_endLevel}" + _fileNameSuffix;
            string newAssetPath = System.IO.Path.GetDirectoryName(assetPath) + "/" + newFileName + ".asset";

            var existingAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<NodeTypeRulesSO>(newAssetPath);
            if (existingAsset != null && existingAsset != this)
            {
                Debug.LogWarningFormat(_duplicateAssetWarning, newFileName);
                UnityEditor.EditorGUIUtility.PingObject(existingAsset);
                return;
            }

            UnityEditor.AssetDatabase.RenameAsset(assetPath, newFileName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }

        [ContextMenu("Populate Node Type Weights")]
        private void PopulateNodeTypeWeights()
        {
            var allNodeTypes = UnityEditor.AssetDatabase.FindAssets("t:MapNodeTypeSO");
            foreach (var guid in allNodeTypes)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var nodeType = UnityEditor.AssetDatabase.LoadAssetAtPath<MapNodeTypeSO>(path);
                if (!_nodeTypeWeights.ContainsKey(nodeType))
                {
                    _nodeTypeWeights.Add(nodeType, 1f);
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }

        [ContextMenu("Add or Remove Missing Node Types in Consecutive Type Weight Reductions")]
        private void AddOrRemoveMissingNodeTypeInConsecutiveTypeWeightReductions()
        {
            var currentWeights = new Dictionary<MapNodeTypeSO, float>(_nodeTypeWeights);

            foreach (var weight in currentWeights)
            {
                if (!_consecutiveTypeWeightReductions.ContainsKey(weight.Key))
                {
                    _consecutiveTypeWeightReductions.Add(weight.Key, weight.Value);
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }

            var entryToRemove = new List<MapNodeTypeSO>();
            foreach (var kvp in _consecutiveTypeWeightReductions)
            {
                if (!currentWeights.ContainsKey(kvp.Key))
                {
                    entryToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in entryToRemove)
            {
                _consecutiveTypeWeightReductions.Remove(key);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

#endif

        #endregion Unity Editor
    }
}