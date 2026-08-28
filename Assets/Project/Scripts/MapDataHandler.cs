using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    [System.Serializable]
    public class MapData
    {
        public int Seed;
        public bool IsCustomSeedUsed;
        public MapTraversalData MapTraversalData;
    }

    [System.Serializable]
    public class MapNodeData
    {
        public int Level;
        public int Index;

        public MapNodeData(MapNode node)
        {
            Level = node.Level;
            Index = node.Index;
        }
    }

    [System.Serializable]
    public class MapTraversalData
    {
        public List<MapNodeData> VisitedNodeDataList = new List<MapNodeData>();
        public MapNodeData CurrentNodeData = null;
        public int TraversalStepsTaken;

        public MapTraversalData(List<MapNode> visitedNodes, MapNode currentNode, int stepsTaken)
        {
            foreach (var node in visitedNodes)
            {
                VisitedNodeDataList.Add(new MapNodeData(node));
            }

            if (currentNode != null)
            {
                CurrentNodeData = new MapNodeData(currentNode);
            }

            TraversalStepsTaken = stepsTaken;
        }
    }

    public class MapDataHandler : MonoBehaviour
    {
        private const string _nullDataError = "Map data is null. Cannot save map.";
        private const string _fileNotFoundError = "Map data file not found at {0}";

        [SerializeField] private string _saveFolder = "Maps/Save";
        [SerializeField] private string _fileName = "GeneratedMapData.json";

        private string FolderPath => System.IO.Path.Combine(Application.persistentDataPath, _saveFolder);
        private string FullFilePath => System.IO.Path.Combine(FolderPath, _fileName);

        #region Public APIs

        public void SaveGame(MapData mapData)
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

        [ContextMenu("Load Map Data")]
        public MapData LoadGame()
        {
            if (!System.IO.File.Exists(FullFilePath))
            {
                Debug.LogErrorFormat(_fileNotFoundError, FullFilePath);
                return new MapData();
            }

            string json = System.IO.File.ReadAllText(FullFilePath);
            return JsonUtility.FromJson<MapData>(json);
        }

        [ContextMenu("Delete Map Data")]
        public void DeleteMapData()
        {
            if (System.IO.File.Exists(FullFilePath))
            {
                System.IO.File.Delete(FullFilePath);
            }
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

        #endregion Public APIs
    }
}