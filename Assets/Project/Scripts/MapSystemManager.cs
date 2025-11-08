using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapSystemManager : MonoBehaviour
    {
        [SerializeField] private MapNodeGenerator _mapGridGenerator;
        [SerializeField] private MapPathGenerator _mapPathGenerator;
        [SerializeField] private MapNodeTypeAssigner _mapNodeTypeAssigner;
        [SerializeField] private MapTraversalController _mapTraversalController;
        [SerializeField] private int _playerInputSeed = 0;
        [SerializeField] private bool _usePlayerInputSeed = false;
        [SerializeField] private int _generationAttempts = 1;

        private MapNode[,] _mapGrid;
        private bool _isCustomSeedUsed;

        [field: SerializeField] private int GeneratedSeed { get; set; } // Set it to private later

        [ContextMenu("Generate New Map")]
        private void Start()
        {
            int attempts = 0;
            Dictionary<int, int> seedViolations = new Dictionary<int, int>();
            do
            {
                GenerateSeed();
                GenerateMap();

                int violations = _mapNodeTypeAssigner.CheckTypeRulesValidity();
                seedViolations[GeneratedSeed] = violations;

                attempts++;
            } while (attempts < _generationAttempts && seedViolations[GeneratedSeed] > 0 && !_usePlayerInputSeed);

            if (seedViolations[GeneratedSeed] > 0)
            {
                RegenerateMapWithBestSeed(seedViolations);
            }

            GenerateMapVisuals();

            // NOTE: Initialize traversal controller after map generation and visuals creation
            // It depends on the onClick events of the node views
            _mapTraversalController.Initialize(_mapGrid, _mapPathGenerator.PathViews);
        }

        private void RegenerateMapWithBestSeed(Dictionary<int, int> seedViolationDict)
        {
            if (_usePlayerInputSeed)
            {
                _mapNodeTypeAssigner.CheckTypeRulesValidity(true);
                Debug.LogWarning($"You're using a custom seed {_playerInputSeed} that resulted in " +
                                 $"{seedViolationDict[GeneratedSeed]} rule violations. " +
                                 $"Consider using a different seed or toggle off the custom seed option" +
                                 $" to allow automatic seed generation with least violations.");
            }
            else
            {
                int bestSeed = GeneratedSeed;
                int leastViolations = seedViolationDict[GeneratedSeed];
                foreach (var kvp in seedViolationDict)
                {
                    if (kvp.Value < leastViolations)
                    {
                        leastViolations = kvp.Value;
                        bestSeed = kvp.Key;
                    }
                }

                GeneratedSeed = bestSeed;

                // Re-generate the map with the best seed
                GenerateMap();
                _mapNodeTypeAssigner.CheckTypeRulesValidity(true);

                Debug.LogWarning($"Could not generate a valid map within {_generationAttempts} attempts. " +
                                 $"Using seed {GeneratedSeed} with {leastViolations} rule violations.");
            }
        }

        private void GenerateSeed()
        {
            if (_usePlayerInputSeed)
            {
                GeneratedSeed = Mathf.Abs(_playerInputSeed);
                _isCustomSeedUsed = true;
            }
            else
            {
                int intMinMaxSeed = Random.Range(int.MinValue, int.MaxValue);
                int dateTimeSeed = System.DateTime.Now.Millisecond;
                GeneratedSeed = Mathf.Abs(intMinMaxSeed + dateTimeSeed);
                _isCustomSeedUsed = false;
            }
        }

        private void GenerateMap()
        {
            // 1. Create map node data
            var mapJitterRNG = new System.Random(GeneratedSeed);
            _mapGridGenerator.Initialize(mapJitterRNG);
            _mapGrid = _mapGridGenerator.CreateNodeGrid();

            // 2. Create map path data
            var mapPathingRNG = new System.Random(GeneratedSeed + 1);
            _mapPathGenerator.Initialize(_mapGrid, mapPathingRNG);
            _mapPathGenerator.SelectStartingNodes();
            _mapPathGenerator.GeneratePaths();
            _mapGridGenerator.ClearUnusedNodes();

            // 3. Assign node types to map node data
            var mapNodeTypeRNG = new System.Random(GeneratedSeed + 2);
            _mapNodeTypeAssigner.Initialize(_mapGrid, mapNodeTypeRNG);
            _mapNodeTypeAssigner.AssignNodeTypes();
        }

        private void GenerateMapVisuals()
        {
            _mapGridGenerator.ClearNodeViews();
            _mapPathGenerator.ClearPathViews();
            _mapTraversalController.ResetTraversalState();

            _mapGridGenerator.CreateNodeViews();
            _mapPathGenerator.CreatePathViews();
        }

        [SerializeField] private string _saveFolder = "Maps/Save";
        [SerializeField] private string _fileName = "GeneratedMapData.json";

        [ContextMenu("Save Map Data to JSON")]
        private void SaveMapDataToJson()
        {
            if (_mapGrid == null)
            {
                Debug.LogError("Map grid is null. Generate the map before saving.");
                return;
            }
            SaveMapData(_mapGrid, GeneratedSeed, _saveFolder, _fileName);
            Debug.Log($"Map data saved to {_saveFolder}/{_fileName}");
        }

        private void SaveMapData(MapNode[,] mapGrid, int seed, string folderPath, string fileName)
        {
            MapData mapDataContainer = new MapData
            {
                Seed = seed,
                Nodes = new List<MapNodeData>(),
                IsCustomSeedUsed = _isCustomSeedUsed
            };

            int levels = mapGrid.GetLength(0);
            int nodesPerLevel = mapGrid.GetLength(1);
            for (int level = 0; level < levels; level++)
            {
                for (int index = 0; index < nodesPerLevel; index++)
                {
                    MapNode node = mapGrid[level, index];
                    if (node != null && node.NodeType != null)
                    {
                        MapNodeData nodeData = new MapNodeData
                        {
                            Level = node.Level,
                            Index = node.Index,
                            NodeTypeID = node.NodeType.ID
                        };
                        mapDataContainer.Nodes.Add(nodeData);
                    }
                }
            }

            string json = JsonUtility.ToJson(mapDataContainer, true);
            string fullPath = System.IO.Path.Combine(Application.dataPath, folderPath);
            if (!System.IO.Directory.Exists(fullPath))
            {
                System.IO.Directory.CreateDirectory(fullPath);
            }

            System.IO.File.WriteAllText(System.IO.Path.Combine(fullPath, fileName), json);
        }

        [ContextMenu("Load Map Data from JSON")]
        private void LoadMapDataFromJson()
        {
            string fullPath = System.IO.Path.Combine(Application.dataPath, _saveFolder, _fileName);
            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogError($"Map data file not found at {fullPath}");
                return;
            }
            string json = System.IO.File.ReadAllText(fullPath);
            MapData mapDataContainer = JsonUtility.FromJson<MapData>(json);
            LoadMapData(mapDataContainer);
            Debug.Log($"Map data loaded from {_saveFolder}/{_fileName}");
        }

        private void LoadMapData(MapData mapData)
        {
            if (mapData == null)
            {
                Debug.LogError("Map data is null. Cannot load map.");
                return;
            }

            if (mapData.IsCustomSeedUsed)
            {
                _usePlayerInputSeed = true;
                _playerInputSeed = mapData.Seed;
            }
            else
            {
                _usePlayerInputSeed = false;
            }

            GeneratedSeed = mapData.Seed;
            GenerateMap();
            // Reassign node types based on loaded data just to be sure
            LoadNodeType(mapData);

            GenerateMapVisuals();
            _mapTraversalController.Initialize(_mapGrid, _mapPathGenerator.PathViews);
        }

        private void LoadNodeType(MapData mapData)
        {
            for (int level = 0; level < _mapGrid.GetLength(0); level++)
            {
                for (int index = 0; index < _mapGrid.GetLength(1); index++)
                {
                    MapNode node = _mapGrid[level, index];
                    if (node == null)
                        continue;

                    MapNodeData loadedNodeData = mapData.Nodes.Find(n => n.Level == level && n.Index == index);
                    if (loadedNodeData == null)
                        continue;

                    var nodeType = _mapNodeTypeAssigner.GetNodeTypeByID(loadedNodeData.NodeTypeID);
                    if (nodeType != null)
                    {
                        node.NodeType = nodeType;
                    }
                    else
                    {
                        Debug.LogWarning($"Node type ID {loadedNodeData.NodeTypeID} not found for node at Level {level}, Index {index}.");
                    }
                }
            }
        }

        [ContextMenu("Open Save Folder")]
        private void OpenSaveFileLocation()
        {
            string fullPath = System.IO.Path.Combine(Application.dataPath, _saveFolder);
            if (!System.IO.Directory.Exists(fullPath))
            {
                Debug.LogError("Save folder does not exist.");
                return;
            }
            Application.OpenURL(fullPath);
        }
    }

    [System.Serializable]
    public class MapData
    {
        public int Seed;
        public List<MapNodeData> Nodes;
        public bool IsCustomSeedUsed;
    }

    [System.Serializable]
    public class MapNodeData
    {
        public int Level;
        public int Index;
        public string NodeTypeID;
    }
}