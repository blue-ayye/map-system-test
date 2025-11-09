using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    [System.Serializable]
    public class MapData
    {
        public int Seed;
        public bool IsCustomSeedUsed;
        public List<MapNodeData> MapNodeDataList;
        public MapTraversalData MapTraversalData;
    }

    [System.Serializable]
    public class MapNodeData
    {
        public int Level;
        public int Index;
        public string NodeTypeID;

        public MapNodeData(MapNode node)
        {
            Level = node.Level;
            Index = node.Index;
            NodeTypeID = node.NodeType.ID;
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
        [SerializeField] private string _saveFolder = "Maps/Save";
        [SerializeField] private string _fileName = "GeneratedMapData.json";

        private string _folderPath => System.IO.Path.Combine(Application.persistentDataPath, _saveFolder);
        private string _fullFilePath => System.IO.Path.Combine(_folderPath, _fileName);

        public void SaveMapData(MapData mapData)
        {
            if (mapData == null)
            {
                Debug.LogError("Map data is null. Cannot save map.");
                return;
            }

            string json = JsonUtility.ToJson(mapData, true);
            if (!System.IO.Directory.Exists(_folderPath))
            {
                System.IO.Directory.CreateDirectory(_folderPath);
            }
            System.IO.File.WriteAllText(_fullFilePath, json);
        }

        public MapData LoadMapData()
        {
            if (!System.IO.File.Exists(_fullFilePath))
            {
                Debug.LogError($"Map data file not found at {_fullFilePath}");
                return new MapData();
            }
            string json = System.IO.File.ReadAllText(_fullFilePath);
            return JsonUtility.FromJson<MapData>(json);
        }
    }
}