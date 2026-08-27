using UnityEngine;

namespace BP.MapSystem
{
    [CreateAssetMenu(fileName = "MapNodeType", menuName = "Map System/Map Node Type")]
    public class MapNodeTypeSO : ScriptableObject
    {
        private const string _fileNameSuffix = "_MapNodeType";
        private const string _duplicateAssetWarning = "An asset with the name '{0}' already exists. Cannot rename.";

        [SerializeField] private string _typeID;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _displayIcon;

        public string DisplayName => _displayName;
        public Sprite DisplayIcon => _displayIcon;
        public string ID => _typeID;

        #region Unity Editor

#if UNITY_EDITOR

        [ContextMenu("Rename File to Match Display Name")]
        public void RenameFile()
        {
            _typeID = _displayName.Replace(" ", "_").ToLower();

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string newFileName = _displayName.Replace(" ", "") + _fileNameSuffix;
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

#endif

        #endregion Unity Editor
    }
}