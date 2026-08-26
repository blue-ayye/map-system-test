using PrimeTween;
using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapSystemManager : MonoBehaviour
    {
        #region Fields and Properties

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

        #endregion Fields and Properties

        #region Unity Lifecycle Methods

        private void Start() => Initialize();

        #endregion Unity Lifecycle Methods

        #region Public APIs

        public void Initialize()
        {
            _mapGridGenerator.CalculateBounds();

            bool foundValidSeed = TryGetValidSeed(out int selectedSeed);

            // Generate the final selected map.
            GenerateMapData(selectedSeed);

            if (!foundValidSeed && _usePlayerInputSeed)
            {
                _mapNodeTypeAssigner.CheckTypeRulesValidity(logging: true);

                Debug.LogWarning(
                    $"The custom seed {_playerInputSeed} generated a map with rule violations. " +
                    "Consider using a different seed or disable the custom seed option."
                );
            }
            else if (!foundValidSeed)
            {
                Debug.LogWarning(
                    $"Could not generate a valid map within {_generationAttempts} attempts. " +
                    $"Using the best available seed: {selectedSeed}."
                );
            }

            GenerateMapVisuals();
            _mapTraversalController.Initialize(_mapGrid, _mapPathGenerator.PathViews);
            AnimateMapReveal();
        }

        private bool TryGetValidSeed(out int selectedSeed)
        {
            selectedSeed = default;

            int attempts = _usePlayerInputSeed
                ? 1
                : Mathf.Max(1, _generationAttempts);

            int bestSeed = default;
            int fewestViolations = int.MaxValue;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int candidateSeed = GenerateSeed();

                GenerateMapData(candidateSeed);

                int violationCount =
                    _mapNodeTypeAssigner.CheckTypeRulesValidity();

                if (violationCount < fewestViolations)
                {
                    fewestViolations = violationCount;
                    bestSeed = candidateSeed;
                }

                if (violationCount == 0)
                {
                    selectedSeed = candidateSeed;
                    return true;
                }
            }

            selectedSeed = bestSeed;
            return false;
        }

        #endregion Public APIs

        private int GenerateSeed()
        {
            if (_usePlayerInputSeed)
            {
                _isCustomSeedUsed = true;
                return Mathf.Abs(_playerInputSeed);
            }

            _isCustomSeedUsed = false;

            int randomSeed = Random.Range(int.MinValue, int.MaxValue);
            int timeSeed = System.DateTime.Now.Millisecond;

            return Mathf.Abs(randomSeed + timeSeed);
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

        private void AnimateMapReveal()
        {
            if (_revealSequence.isAlive)
            {
                _revealSequence.Stop();
            }

            _revealSequence = Sequence.Create();

            for (int level = 0; level < _mapGrid.GetLength(0); level++)
            {
                // 1. POP IN NODES (Waits for previous level's paths to finish)
                bool firstNodeChained = false;
                for (int index = 0; index < _mapGrid.GetLength(1); index++)
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
            if (mapData == null)
            {
                Debug.LogError("Map data is null. Cannot load map.");
                return;
            }

            ReadFrom(mapData);
            GenerateMapData(mapData.Seed);

            // Reassign node types based on loaded data just to be sure
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
    }
}