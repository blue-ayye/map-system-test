using UnityEngine;

namespace BP.MapSystem
{
    /// <summary>
    /// Handles saving and loading of map data to and from a JSON file in the specified save folder. Provides methods to save, load, delete, and open the save folder for map data.
    /// </summary>
    public class MapDataHandler : MonoBehaviour
    {
        [SerializeField] private string _saveFolder = "Maps/Save";
        [SerializeField] private string _fileName = "GeneratedMapData.json";

        private const string _nullDataError = "Map data is null. Cannot save map.";
        private const string _fileNotFoundError = "Map data file not found at {0}";

        private string FolderPath => System.IO.Path.Combine(Application.persistentDataPath, _saveFolder);
        private string FullFilePath => System.IO.Path.Combine(FolderPath, _fileName);

        #region Public APIs

        /// <summary>
        /// Saves the provided map data to a JSON file in the specified save folder.
        /// </summary>
        /// <param name="mapData">The map data to save.</param>
        public void SaveGame(MapData mapData) => SaveGame_Internal(mapData);

        /// <summary>
        /// Loads the map data from the JSON file in the specified save folder.
        /// </summary>
        /// <returns>The loaded map data.</returns>
        public MapData LoadGame() => LoadGame_Internal();

        /// <summary>
        /// Deletes the map data JSON file from the specified save folder.
        /// </summary>
        public void DeleteMapData() => DeleteMapData_Internal();

        /// <summary>
        /// Opens the save folder in the file explorer.
        /// </summary>
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