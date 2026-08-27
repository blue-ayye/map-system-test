using PrimeTween;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapSystemManager : MonoBehaviour
    {
        [SerializeField] private MapNodeGenerator _mapGridGenerator;
        [SerializeField] private MapPathGenerator _mapPathGenerator;
        [SerializeField] private MapNodeTypeAssigner _mapNodeTypeAssigner;
        [SerializeField] private MapTraversalController _mapTraversalController;
        [SerializeField] private MapDataHandler _mapDataHandler;
        [SerializeField] private int _playerInputSeed = 0;
        [SerializeField] private bool _usePlayerInputSeed = false;
        [SerializeField] private int _generationAttempts = 1;

        [Header("Animation Settings")]
        [SerializeField] private float _nodeSpawnDuration = 0;
        [SerializeField] private float _pathDrawDuration = 0;

        private MapNode[,] _mapGrid;
        private bool _isCustomSeedUsed;
        private int _generatedSeed;
        private Sequence _revealSequence;

        private const string _customSeedWarning = "The custom seed {0} generated a map with rule violations. Consider using a different seed or disable the custom seed option.";
        private const string _generationAttemptsWarning = "Could not generate a valid map within {0} attempts. Using the best available seed: {1}.";
        private const string _nullMapDataError = "Map data is null or empty. Cannot load map.";

        #region Unity API

        private void Start() => GemerateMap();

        #endregion Unity API

        #region Public APIs

        [ContextMenu("Generate Map")]
        public void GemerateMap()
        {
            _mapGridGenerator.CalculateBounds();

            bool foundValidSeed = GenerateValidMapData();

            if (!foundValidSeed && _usePlayerInputSeed)
            {
                _mapNodeTypeAssigner.CheckTypeRulesValidity(logging: true);
                Debug.LogWarningFormat(_customSeedWarning, _playerInputSeed);
            }
            else if (!foundValidSeed)
            {
                Debug.LogWarningFormat(_generationAttemptsWarning, _generationAttempts, _generatedSeed);
            }

            GenerateMapVisuals();
            _mapTraversalController.Initialize(_mapGrid, _mapPathGenerator.PathViews);
            AnimateMapReveal();
        }

        #endregion Public APIs

        #region Map Generation

        private bool GenerateValidMapData()
        {
            int attempts = _usePlayerInputSeed
                ? 1
                : Mathf.Max(1, _generationAttempts);

            int bestSeed = default;
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

            // If we reach this point, all attempts failed to produce a 0-violation map.
            // The current class state belongs to the last failed attempt in the loop.
            // We must regenerate the data one final time using the best seed we found.
            GenerateMapData(bestSeed);

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

            // Simplified to avoid integer overflow issues when combining two large integers
            return Random.Range(0, int.MaxValue);
        }

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

            _mapGridGenerator.ClearUnusedNodes();

            // 3. Assign node types to map node data
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

        #region Animation

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
                        Tween pathTween = pathView.AnimateDraw(_pathDrawDuration);

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

        #region Map Data Management

        [ContextMenu("Map Data/Save")]
        private void SaveMap()
        {
            var mapData = new MapData();
            WriteTo(mapData);
            _mapGridGenerator.WriteTo(mapData);
            _mapTraversalController.WriteTo(mapData);

            _mapDataHandler.SaveMapData(mapData);
        }

        [ContextMenu("Map Data/Load")]
        private void LoadMap()
        {
            var mapData = _mapDataHandler.LoadMapData();

            if (mapData == null || mapData.MapNodeDataList == null || mapData.MapNodeDataList.Count == 0)
            {
                Debug.LogError(_nullMapDataError);
                return;
            }

            ReadFrom(mapData);
            GenerateMapData(mapData.Seed);

            _mapNodeTypeAssigner.ReadFrom(mapData);

            GenerateMapVisuals();

            _mapTraversalController.Initialize(_mapGrid, _mapPathGenerator.PathViews);
            _mapTraversalController.ReadFrom(mapData);

            AnimateMapReveal();
        }

        private void WriteTo(MapData mapData)
        {
            mapData.Seed = _generatedSeed;
            mapData.IsCustomSeedUsed = _isCustomSeedUsed;
        }

        private void ReadFrom(MapData mapData)
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
    }
}