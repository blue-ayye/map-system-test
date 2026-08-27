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
        private const string _fileNameSuffix = "_NodeTypeRules";
        private const string _duplicateAssetWarning = "An asset with the name '{0}' already exists. Cannot rename.";

        [SerializeField] private int _startLevel;
        [SerializeField] private int _endLevel;
        [SerializeField] private bool _excludeFromOtherRules;
        [SerializedDictionary("Node Type", "Weight")]
        [SerializeField] private SerializedDictionary<MapNodeTypeSO, float> _nodeTypeWeights;
        [SerializedDictionary("Node Type", "Weight Reduction")]
        [SerializeField] private SerializedDictionary<MapNodeTypeSO, float> _consecutiveTypeWeightReductions;
        [SerializeField] private SiblingNodeTypeConstraint _siblingNodeTypeConstraint;

        public int StartLevel => _startLevel;
        public int EndLevel => _endLevel;
        public bool ExcludeFromOtherRules => _excludeFromOtherRules;
        public Dictionary<MapNodeTypeSO, float> NodeTypeWeights => _nodeTypeWeights;
        public Dictionary<MapNodeTypeSO, float> ConsecutiveTypeWeightReductions => _consecutiveTypeWeightReductions;
        public SiblingNodeTypeConstraint SiblingTypeConstraint => _siblingNodeTypeConstraint;

        #region Unity Editor

#if UNITY_EDITOR

        [ContextMenu("Rename File to Match Display Name")]
        public void RenameFile()
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string newFileName = $"{_startLevel}-{_endLevel}" + _fileNameSuffix;
            string newAssetPath = System.IO.Path.GetDirectoryName(assetPath) + "/" + newFileName + ".asset";

            var existingAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<MapNodeTypeSO>(newAssetPath);
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
        private void AddOrRemoveMissingNodeTypeInConsecutiveTypeWightReductions()
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