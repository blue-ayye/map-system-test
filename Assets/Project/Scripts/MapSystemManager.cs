using PrimeTween;
using UnityEngine;

namespace BP.MapSystem
{
    /// <summary>
    /// Manages the overall map system, including generation, saving/loading, and animation of the map. 
    /// It coordinates between different components responsible for generating nodes, paths, assigning node types, and handling traversal. 
    /// It also provides public APIs for generating a new map, saving the current map state, and loading a previously saved map state.
    /// </summary>
    public class MapSystemManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapNodeGenerator _mapGridGenerator;
        [SerializeField] private MapPathGenerator _mapPathGenerator;
        [SerializeField] private MapNodeTypeAssigner _mapNodeTypeAssigner;
        [SerializeField] private MapTraversalController _mapTraversalController;
        [SerializeField] private MapDataHandler _mapDataHandler;

        [Header("Map Generation Settings")]
        [SerializeField] private int _playerInputSeed = 0;
        [SerializeField] private bool _usePlayerInputSeed = false;
        [SerializeField] private int _generationAttempts = 1;

        [Header("Animation Settings")]
        [SerializeField] private float _nodeSpawnDuration = 0.001f;
        [SerializeField] private float _pathDrawDuration = 0.001f;

        private MapNode[,] _mapGrid;
        private bool _isCustomSeedUsed;
        private int _generatedSeed;
        private Sequence _revealSequence;

        private const string _customSeedWarning = "The custom seed {0} generated a map with rule violations. Consider using a different seed or disable the custom seed option.";
        private const string _generationAttemptsWarning = "Could not generate a valid map within {0} attempts. Using the best available seed: {1}.";
        private const string _nullMapDataError = "Map data is null or empty. Cannot load map.";

        #region Unity API

        private void Start() => GenerateMap();

        #endregion Unity API

        #region Public APIs

        /// <summary>
        /// Generates a new map based on the current settings.
        /// If a custom seed is used, it will attempt to generate a valid map with that seed.
        /// If not, it will try multiple attempts to find a valid map and use the best seed found.
        /// </summary>
        [ContextMenu("Generate Map")]
        public void GenerateMap() => GenerateMap_Internal();

        [ContextMenu("Save Map")]
        public void SaveMap() => StartSavingGame();

        [ContextMenu("Load Map")]
        public void LoadMap() => StartLoadingGame();

        #endregion Public APIs

        #region Map Generation

        private void GenerateMap_Internal()
        {
            _mapGridGenerator.CalculateBounds();

            if (!TryGenerateValidMapData(out int bestSeed))
            {
                // Log the details before regenerating the map data with the best seed found.
                if (_usePlayerInputSeed)
                {
                    _mapNodeTypeAssigner.CheckTypeRulesValidity(logging: true);
                    Debug.LogWarningFormat(_customSeedWarning, _playerInputSeed);
                }
                else
                {
                    Debug.LogWarningFormat(_generationAttemptsWarning, _generationAttempts, _generatedSeed);
                }

                // If we reach this point, all attempts failed to produce a 0-violation map.
                // The current class state belongs to the last failed attempt in the loop.
                // We must regenerate the data one final time using the best seed we found.
                GenerateMapData(bestSeed);
            }

            GenerateMapVisuals();
            _mapTraversalController.ConnectMapVisuals(_mapGrid, _mapPathGenerator.PathViews);
            AnimateMapReveal();
        }

        /// <summary>
        /// Attempts to generate valid map data based on the current settings.
        /// </summary>
        /// <param name="bestSeed">The best seed found during the generation attempts.</param>
        /// <returns>True if a valid map was generated with zero rule violations; otherwise, false.</returns>
        private bool TryGenerateValidMapData(out int bestSeed)
        {
            int attempts = _usePlayerInputSeed
                ? 1
                : Mathf.Max(1, _generationAttempts);

            bestSeed = default;
            int fewestViolations = int.MaxValue;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int candidateSeed = GenerateSeed();

                // This mutates the class state (nodes, grids, paths)
                GenerateMapData(candidateSeed);

                int violationCount = _mapNodeTypeAssigner.CheckTypeRulesValidity();

                if (violationCount < fewestViolations)
                {
                    fewestViolations = violationCount;
                    bestSeed = candidateSeed;
                }

                if (violationCount == 0)
                {
                    // A perfect map was generated. The class state is already correct,
                    // so we can exit immediately without regenerating.
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Generates a seed for map generation based on the current settings.
        /// </summary>
        /// <returns>The generated seed.</returns>
        private int GenerateSeed()
        {
            if (_usePlayerInputSeed)
            {
                _isCustomSeedUsed = true;
                return Mathf.Abs(_playerInputSeed);
            }

            _isCustomSeedUsed = false;

            // Simplified to avoid integer overflow issues when combining two large integers
            return Random.Range(0, int.MaxValue);
        }

        /// <summary>
        /// Generates pure logical map data (nodes, paths, and node types) based on the provided seed.
        /// This does not create any visual representations.
        /// </summary>
        /// <param name="seed">The seed to use for map generation.</param>
        private void GenerateMapData(int seed)
        {
            _generatedSeed = seed;

            // 1. Create map node data
            var mapJitterRNG = new System.Random(seed);
            _mapGridGenerator.Initialize(mapJitterRNG);
            _mapGrid = _mapGridGenerator.CreateNodeGrid();

            // 2. Create map path data
            var mapPathingRNG = new System.Random(seed + 1);
            _mapPathGenerator.Initialize(_mapGrid, mapPathingRNG);
            _mapPathGenerator.SelectStartingNodes();
            _mapPathGenerator.GeneratePaths();

            // 3. Clear any unused nodes from the grid (nodes that are not part of any path)
            // We do this after generating paths to ensure that only nodes that are part of the generated paths remain in the grid.
            _mapGridGenerator.ClearUnusedNodes();

            // 4. Assign node types to map node data
            var mapNodeTypeRNG = new System.Random(seed + 2);
            _mapNodeTypeAssigner.Initialize(_mapGrid, mapNodeTypeRNG);
            _mapNodeTypeAssigner.AssignNodeTypes();
        }

        /// <summary>
        /// Generates the visual representations of the map (nodes and paths) based on the current map data.
        /// </summary>
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

        private void StartSavingGame()
        {
            var mapData = new MapData();
            this.WriteToMapData(mapData);
            _mapTraversalController.WriteToMapData(mapData);

            _mapDataHandler.SaveGame(mapData);
        }

        private void StartLoadingGame()
        {
            var mapData = _mapDataHandler.LoadGame();

            if (mapData == null)
            {
                Debug.LogError(_nullMapDataError);
                return;
            }

            this.ReadFromMapData(mapData);

            GenerateMapData(mapData.Seed);
            GenerateMapVisuals();

            _mapTraversalController.ConnectMapVisuals(_mapGrid, _mapPathGenerator.PathViews);
            _mapTraversalController.ReadFromMapData(mapData);

            AnimateMapReveal();
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

        /// <summary>
        /// Animates the reveal of the map, including nodes and paths.
        /// </summary>
        private void AnimateMapReveal()
        {
            if (_revealSequence.isAlive)
            {
                _revealSequence.Stop();
            }

            _revealSequence = Sequence.Create();

            int levels = _mapGrid.GetLength(0);
            int nodesPerLevel = _mapGrid.GetLength(1);

            for (int level = 0; level < levels; level++)
            {
                // 1. POP IN NODES (Waits for previous level's paths to finish)
                bool firstNodeChained = false;
                for (int index = 0; index < nodesPerLevel; index++)
                {
                    var node = _mapGrid[level, index];
                    if (node != null && node.NodeView != null)
                    {
                        Tween nodeTween = node.NodeView.AnimateSpawn(_nodeSpawnDuration);

                        if (!firstNodeChained)
                        {
                            _revealSequence.Chain(nodeTween);
                            firstNodeChained = true;
                        }
                        else
                        {
                            _revealSequence.Group(nodeTween);
                        }
                    }
                }

                // 2. DRAW PATHS (Waits for THIS level's nodes to finish popping in)
                bool firstPathChained = false;
                foreach (var pathView in _mapPathGenerator.PathViews)
                {
                    if (pathView.FromNode.Level == level)
                    {
                        Tween pathTween = pathView.AnimateInitialDraw(_pathDrawDuration);

                        if (!firstPathChained)
                        {
                            _revealSequence.Chain(pathTween);
                            firstPathChained = true;
                        }
                        else
                        {
                            _revealSequence.Group(pathTween);
                        }
                    }
                }
            }
        }

        #endregion Animation
    }
}