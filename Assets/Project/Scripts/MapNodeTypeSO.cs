using UnityEngine;

namespace BP.MapSystem
{
    [CreateAssetMenu(fileName = "MapNodeType", menuName = "BP/Map System/Map Node Type")]
    public class MapNodeTypeSO : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _displayIcon;

        public string DisplayName => _displayName;
        public Sprite DisplayIcon => _displayIcon;

#if UNITY_EDITOR

        [ContextMenu("Rename File to Match Display Name")]
        public void RenameFile()
        {
            string suffix = "_MapNodeType";
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string newFileName = _displayName.Replace(" ", "") + suffix;
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