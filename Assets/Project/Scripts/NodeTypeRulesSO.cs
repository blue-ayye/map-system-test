using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;

namespace BP.MapSystem
{
    [CreateAssetMenu(fileName = "NodeTypeRules", menuName = "Map System/Node Type Rules")]
    public class NodeTypeRulesSO : ScriptableObject
    {
        [SerializeField] private int _startLevel;
        [SerializeField] private int _endLevel;
        [SerializeField] private bool _excludeFromOtherRules;
        [SerializedDictionary("Node Type", "Weight")]
        [SerializeField] private SerializedDictionary<MapNodeTypeSO, float> _nodeTypeWeights;
        //[SerializeField] private float _parentTypeWeightReductionFactor = 0f;
        [SerializedDictionary("Node Type", "Weight Reduction")]
        [SerializeField] private SerializedDictionary<MapNodeTypeSO, float> _consecutiveTypeWeightReductions;

        public int StartLevel => _startLevel;
        public int EndLevel => _endLevel;
        public bool ExcludeFromOtherRules => _excludeFromOtherRules;
        public Dictionary<MapNodeTypeSO, float> NodeTypeWeights => _nodeTypeWeights;
        //public float ParentTypeWeightReductionFactor => _parentTypeWeightReductionFactor;
        public Dictionary<MapNodeTypeSO, float> ConsecutiveTypeWeightReductions => _consecutiveTypeWeightReductions;

#if UNITY_EDITOR

        [ContextMenu("Rename File to Match Display Name")]
        public void RenameFile()
        {
            string suffix = "_NodeTypeRules";
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string newFileName = $"{_startLevel}-{_endLevel}" + suffix;
            string newAssetPath = System.IO.Path.GetDirectoryName(assetPath) + "/" + newFileName + ".asset";

            // Check if an asset with the new name already exists
            var existingAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<MapNodeTypeSO>(newAssetPath);
            if (existingAsset != null && existingAsset != this)
            {
                Debug.LogWarning($"An asset with the name '{newFileName}' already exists. Cannot rename.");
                UnityEditor.EditorGUIUtility.PingObject(existingAsset);
                return;
            }

            UnityEditor.AssetDatabase.RenameAsset(assetPath, newFileName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }

        [ContextMenu("Fill Consecutive Type Reductions")]
        private void FillConsecutiveTypeReductions()
        {
            var allNodeTypes = UnityEditor.AssetDatabase.FindAssets("t:MapNodeTypeSO");
            foreach (var guid in allNodeTypes)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var nodeType = UnityEditor.AssetDatabase.LoadAssetAtPath<MapNodeTypeSO>(path);
                if (!_consecutiveTypeWeightReductions.ContainsKey(nodeType))
                {
                    _consecutiveTypeWeightReductions.Add(nodeType, 0f);
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }

#endif
    }
}