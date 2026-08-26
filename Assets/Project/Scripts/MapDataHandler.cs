using UnityEngine;

namespace BP.MapSystem
{
    /// <summary>
    /// Pure I/O service for persisting and retrieving <see cref="MapData"/>.
    /// This class has no knowledge of map generation logic; it only serialises
    /// and deserialises the save payload.
    /// </summary>
    public class MapDataHandler : MonoBehaviour
    {
        #region Fields

        [SerializeField] private string _saveFolder = "Maps/Save";
        [SerializeField] private string _fileName = "GeneratedMapData.json";

        #endregion

        #region Properties

        private string FolderPath => System.IO.Path.Combine(Application.persistentDataPath, _saveFolder);
        private string FullFilePath => System.IO.Path.Combine(FolderPath, _fileName);

        #endregion

        #region Public API

        /// <summary>Serialises <paramref name="mapData"/> to disk as JSON.</summary>
        public void SaveMapData(MapData mapData)
        {
            if (mapData == null)
            {
                Debug.LogError("Map data is null. Cannot save map.");
                return;
            }

            string json = JsonUtility.ToJson(mapData, true);
            if (!System.IO.Directory.Exists(FolderPath))
            {
                System.IO.Directory.CreateDirectory(FolderPath);
            }
            System.IO.File.WriteAllText(FullFilePath, json);
        }

        /// <summary>
        /// Loads and deserialises <see cref="MapData"/> from disk.
        /// Returns <c>null</c> if the save file does not exist.
        /// </summary>
        public MapData LoadMapData()
        {
            if (!System.IO.File.Exists(FullFilePath))
            {
                Debug.LogError($"Map data file not found at {FullFilePath}");
                return null;
            }
            string json = System.IO.File.ReadAllText(FullFilePath);
            return JsonUtility.FromJson<MapData>(json);
        }

        #endregion
    }
}