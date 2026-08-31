using UnityEngine;

namespace BP.MapSystem
{
    public class MapDataHandler : MonoBehaviour
    {
        [SerializeField] private string _saveFolder = "Maps/Save";
        [SerializeField] private string _fileName = "GeneratedMapData.json";

        private const string _nullDataError = "Map data is null. Cannot save map.";
        private const string _fileNotFoundError = "Map data file not found at {0}";

        private string FolderPath => System.IO.Path.Combine(Application.persistentDataPath, _saveFolder);
        private string FullFilePath => System.IO.Path.Combine(FolderPath, _fileName);

        #region Public APIs

        public void SaveGame(MapData mapData) => SaveGame_Internal(mapData);

        public MapData LoadGame() => LoadGame_Internal();

        [ContextMenu("Delete Map Data")]
        public void DeleteMapData() => DeleteMapData_Internal();

        [ContextMenu("Open Save Folder")]
        public void OpenSaveFolder() => OpenSaveFolder_Internal();

        #endregion Public APIs

        #region File Operations

        private void SaveGame_Internal(MapData mapData)
        {
            if (mapData == null)
            {
                Debug.LogError(_nullDataError);
                return;
            }

            string json = JsonUtility.ToJson(mapData, true);

            if (!System.IO.Directory.Exists(FolderPath))
            {
                System.IO.Directory.CreateDirectory(FolderPath);
            }

            System.IO.File.WriteAllText(FullFilePath, json);
        }

        private MapData LoadGame_Internal()
        {
            if (!System.IO.File.Exists(FullFilePath))
            {
                Debug.LogErrorFormat(_fileNotFoundError, FullFilePath);
                return new MapData();
            }

            string json = System.IO.File.ReadAllText(FullFilePath);
            return JsonUtility.FromJson<MapData>(json);
        }

        private void DeleteMapData_Internal()
        {
            if (System.IO.File.Exists(FullFilePath))
            {
                System.IO.File.Delete(FullFilePath);
            }
        }

        private void OpenSaveFolder_Internal()
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