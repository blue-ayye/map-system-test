using PrimeTween;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapSystemManager : MonoBehaviour
    {
        [Header("System References")]
        [Tooltip("The root transform for all map visuals.")]
        [SerializeField] private Transform _mapContainer;
        [Tooltip("Handles the mathematical grid generation and node spawning.")]
        [SerializeField] private MapNodeGenerator _mapGridGenerator;
        [Tooltip("Handles route connections between nodes across the grid.")]
        [SerializeField] private MapPathGenerator _mapPathGenerator;
        [Tooltip("Applies procedural rules to assign types (e.g., combat, shop) to nodes.")]
        [SerializeField] private MapNodeTypeAssigner _mapNodeTypeAssigner;
        [Tooltip("Manages player movement state across the generated map.")]
        [SerializeField] private MapTraversalController _mapTraversalController;
        [Tooltip("Handles local disk read/write operations for map states.")]
        [SerializeField] private MapDataHandler _mapDataHandler;

        [Header("Generation Parameters")]
        [Tooltip("Enable to force the map generator to use a specific seed.")]
        [SerializeField] private bool _usePlayerInputSeed = false;
        [Tooltip("The explicit seed used if custom seed is enabled.")]
        [SerializeField] private int _playerInputSeed = 0;
        [Tooltip("How many times the generator should attempt to build a valid map before falling back to the best available configuration.")]
        [SerializeField] private int _generationAttempts = 1;

        private MapNode[,] _mapGrid;
        private bool _isCustomSeedUsed;
        private int _generatedSeed;
        private Sequence _revealSequence;

        private const string _customSeedWarning = "The custom seed {0} generated a map with rule violations. Consider using a different seed or disable the custom seed option.";
        private const string _generationAttemptsWarning = "Could not generate a valid map within {0} attempts. Using the best available seed: {1}.";
        private const string _nullMapDataError = "Map data is null or empty. Cannot load map.";

        public int PlayerInputSeed { get => _playerInputSeed; set => _playerInputSeed = value; }
        public int GeneratedSeed => _generatedSeed;
        public bool UsePlayerInputSeed { get => _usePlayerInputSeed; set => _usePlayerInputSeed = value; }
        public int GenerationAttempts { get => _generationAttempts; set => _generationAttempts = value; }
        public Transform MapContainer => _mapContainer;

        #region Unity API

        private void Start() => GenerateMap();

        #endregion Unity API

        #region Map Generation

        [ContextMenu("Generate Map")]
        public void GenerateMap()
        {
            _mapGridGenerator.CalculateBounds();

            if (!TryGenerateValidMapData(out int bestSeed))
            {
                if (_usePlayerInputSeed)
                {
                    _mapNodeTypeAssigner.CheckTypeRulesValidity(logging: true);
                    Debug.LogWarningFormat(_customSeedWarning, _playerInputSeed);
                }
                else
                {
                    Debug.LogWarningFormat(_generationAttemptsWarning, _generationAttempts, _generatedSeed);
                }

                // Regenerate the map with the best seed found during attempts
                GenerateMapData(bestSeed);
            }

            GenerateMapVisuals();
            _mapTraversalController.ConnectMapVisuals(_mapGrid, _mapPathGenerator.PathViews, _mapGridGenerator.InitialNode, _mapGridGenerator.FinalNode);
            AnimateMapReveal();
        }

        private bool TryGenerateValidMapData(out int bestSeed)
        {
            int attempts = _usePlayerInputSeed ? 1 : Mathf.Max(1, _generationAttempts);
            bestSeed = default;
            int fewestViolations = int.MaxValue;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int candidateSeed = GenerateSeed();
                GenerateMapData(candidateSeed);

                int violationCount = _mapNodeTypeAssigner.CheckTypeRulesValidity();

                if (violationCount < fewestViolations)
                {
                    fewestViolations = violationCount;
                    bestSeed = candidateSeed;
                }

                if (violationCount == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private int GenerateSeed()
        {
            if (_usePlayerInputSeed)
            {
                _isCustomSeedUsed = true;
                return Mathf.Abs(_playerInputSeed);
            }

            _isCustomSeedUsed = false;
            return Random.Range(0, int.MaxValue);
        }

        private void GenerateMapData(int seed)
        {
            _generatedSeed = seed;

            var mapJitterRNG = new System.Random(seed);
            _mapGridGenerator.Initialize(mapJitterRNG);
            _mapGrid = _mapGridGenerator.CreateNodeGrid();

            var mapPathingRNG = new System.Random(seed + 1);
            _mapPathGenerator.Initialize(_mapGrid, mapPathingRNG, _mapGridGenerator.InitialNode, _mapGridGenerator.FinalNode);
            _mapPathGenerator.SelectStartingNodes();
            _mapPathGenerator.GeneratePaths();

            _mapGridGenerator.ClearUnusedNodes();

            var mapNodeTypeRNG = new System.Random(seed + 2);
            _mapNodeTypeAssigner.Initialize(_mapGrid, mapNodeTypeRNG);
            _mapNodeTypeAssigner.AssignNodeTypes();
        }

        private void GenerateMapVisuals()
        {
            _mapTraversalController.ClearSubscriptions();

            _mapGridGenerator.ClearNodeViews();
            _mapPathGenerator.ClearPathViews();
            _mapTraversalController.ResetTraversalState();

            _mapGridGenerator.CreateNodeViews();
            _mapPathGenerator.CreatePathViews();
        }

        #endregion Map Generation

        #region Map Data Management

        [ContextMenu("Save Map")]
        public void SaveMap()
        {
            var mapData = new MapData();
            WriteToMapData(mapData);
            _mapTraversalController.WriteToMapData(mapData);
            _mapDataHandler.SaveGame(mapData);
        }

        [ContextMenu("Load Map")]
        public void LoadMap()
        {
            var mapData = _mapDataHandler.LoadGame();

            if (mapData == null)
            {
                Debug.LogWarning(_nullMapDataError);
                return;
            }

            ReadFromMapData(mapData);

            GenerateMapData(mapData.Seed);
            GenerateMapVisuals();

            _mapTraversalController.ConnectMapVisuals(_mapGrid, _mapPathGenerator.PathViews, _mapGridGenerator.InitialNode, _mapGridGenerator.FinalNode);
            _mapTraversalController.ReadFromMapData(mapData);

            AnimateMapReveal();
        }

        [ContextMenu("Delete Map Save")]
        public void DeleteMapSave()
        {
            _mapDataHandler.DeleteMapData();
        }

        private void WriteToMapData(MapData mapData)
        {
            mapData.Seed = _generatedSeed;
            mapData.IsCustomSeedUsed = _isCustomSeedUsed;
        }

        private void ReadFromMapData(MapData mapData)
        {
            if (mapData.IsCustomSeedUsed)
            {
                _usePlayerInputSeed = true;
                _playerInputSeed = mapData.Seed;
            }
            else
            {
                _usePlayerInputSeed = false;
            }
            _generatedSeed = mapData.Seed;
        }

        #endregion Map Data Management

        #region Animation

        private void AnimateMapReveal()
        {
            if (_revealSequence.isAlive) _revealSequence.Stop();

            _revealSequence = Sequence.Create();

            // 1. Initial Node
            if (_mapGridGenerator.InitialNode != null)
            {
                _mapGridGenerator.AppendNodeSpawnToSequence(ref _revealSequence, _mapGridGenerator.InitialNode);
                _mapPathGenerator.AppendLevelPathsToSequence(ref _revealSequence, _mapGridGenerator.InitialNode.Level);
            }

            // 2. Main Grid
            int levels = _mapGrid.GetLength(0);
            for (int level = 0; level < levels; level++)
            {
                _mapGridGenerator.AppendLevelNodesToSequence(ref _revealSequence, level);
                _mapPathGenerator.AppendLevelPathsToSequence(ref _revealSequence, level);
            }

            // 3. Final Node
            if (_mapGridGenerator.FinalNode != null)
            {
                _mapGridGenerator.AppendNodeSpawnToSequence(ref _revealSequence, _mapGridGenerator.FinalNode);
            }

            // 4. Animate saved states
            _mapTraversalController.AppendRestoredTraversalsToSequence(ref _revealSequence);
        }

        #endregion Animation
    }
}