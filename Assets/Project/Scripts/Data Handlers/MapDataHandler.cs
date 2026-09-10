using UnityEngine;

namespace BP.MapSystem
{
    public class MapDataHandler : MonoBehaviour
    {
        [Header("Storage Configuration")]
        [Tooltip("The sub-folder path inside persistentDataPath where map saves are stored.")]
        [SerializeField] private string _saveFolder = "Maps/Save";
        [Tooltip("The explicit filename used for the serialized map JSON.")]
        [SerializeField] private string _fileName = "GeneratedMapData.json";

        private const string _nullDataError = "Map data is null. Cannot save map.";
        private const string _fileNotFoundError = "Map data file not found at {0}";

        private string FolderPath => System.IO.Path.Combine(Application.persistentDataPath, _saveFolder);
        private string FullFilePath => System.IO.Path.Combine(FolderPath, _fileName);

        #region File Operations

        public void SaveGame(MapData mapData)
        {
            if (mapData == null)
            {
                Debug.LogWarning(_nullDataError);
                return;
            }

            string json = JsonUtility.ToJson(mapData, true);

            if (!System.IO.Directory.Exists(FolderPath))
            {
                System.IO.Directory.CreateDirectory(FolderPath);
            }

            System.IO.File.WriteAllText(FullFilePath, json);
        }

        public MapData LoadGame()
        {
            if (!System.IO.File.Exists(FullFilePath))
            {
                Debug.LogWarningFormat(_fileNotFoundError, FullFilePath);
                return null;
            }

            string json = System.IO.File.ReadAllText(FullFilePath);
            return JsonUtility.FromJson<MapData>(json);
        }

        [ContextMenu("Delete Map Data")]
        public void DeleteMapData()
        {
            if (!System.IO.File.Exists(FullFilePath))
            {
                Debug.LogWarningFormat(_fileNotFoundError, FullFilePath);
                return;
            }

            System.IO.File.Delete(FullFilePath);
        }

        [ContextMenu("Open Save Folder")]
        public void OpenSaveFolder()
        {
            if (!System.IO.Directory.Exists(FolderPath))
            {
                System.IO.Directory.CreateDirectory(FolderPath);
            }

            Application.OpenURL(FolderPath);
        }

        #endregion File Operations
    }
}