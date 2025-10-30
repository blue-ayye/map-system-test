using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    [CreateAssetMenu(fileName = "NodeTypeRules", menuName = "Map System/Node Type Rules")]
    public class NodeTypeRulesSO : ScriptableObject
    {
        [SerializeField] private int _startLevel;
        [SerializeField] private int _endLevel;
        [SerializeField] private List<NodeTypeFloatValue> _nodeTypeWeights;
        [Tooltip("0 = no reduction, 1 = full reduction")]
        [SerializeField] private float _parentTypeWeightReductionFactor = 0f;

        public int StartLevel => _startLevel;
        public int EndLevel => _endLevel;
        public List<NodeTypeFloatValue> NodeTypeWeights => _nodeTypeWeights;
        public float ParentTypeWeightReductionFactor => _parentTypeWeightReductionFactor;

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

#endif
    }
}