using PrimeTween;
using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
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
                if (_usePlayerInputSeed)
                {
                    _mapNodeTypeAssigner.CheckTypeRulesValidity(logging: true);
                    Debug.LogWarningFormat(_customSeedWarning, _playerInputSeed);
                }
                else
                {
                    Debug.LogWarningFormat(_generationAttemptsWarning, _generationAttempts, _generatedSeed);
                }

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

            _mapTraversalController.ConnectMapVisuals(_mapGrid, _mapPathGenerator.PathViews, _mapGridGenerator.InitialNode, _mapGridGenerator.FinalNode);
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

        private void AnimateMapReveal()
        {
            if (_revealSequence.isAlive)
            {
                _revealSequence.Stop();
            }

            _revealSequence = Sequence.Create();

            int levels = _mapGrid.GetLength(0);
            int nodesPerLevel = _mapGrid.GetLength(1);

            void ChainPathsForLevel(int targetLevel)
            {
                bool firstPathChained = false;
                foreach (var pathView in _mapPathGenerator.PathViews)
                {
                    if (pathView.FromNode.Level == targetLevel)
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

            if (_mapGridGenerator.InitialNode?.NodeView != null)
            {
                _revealSequence.Chain(_mapGridGenerator.InitialNode.NodeView.AnimateSpawn(_nodeSpawnDuration));
                ChainPathsForLevel(_mapGridGenerator.InitialNode.Level);
            }

            for (int level = 0; level < levels; level++)
            {
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

                ChainPathsForLevel(level);
            }

            if (_mapGridGenerator.FinalNode?.NodeView != null)
            {
                _revealSequence.Chain(_mapGridGenerator.FinalNode.NodeView.AnimateSpawn(_nodeSpawnDuration));
            }

            var traversedEdges = _mapTraversalController.TraversedEdges;
            if (traversedEdges.Count > 0)
            {
                _revealSequence.Chain(Tween.Delay(0.3f));

                var animatedPaths = new HashSet<(MapNode, MapNode)>();

                foreach (var edge in traversedEdges)
                {
                    if (!animatedPaths.Contains((edge.From, edge.To)) &&
                        !animatedPaths.Contains((edge.To, edge.From)))
                    {
                        animatedPaths.Add((edge.From, edge.To));

                        Tween pathTween = _mapTraversalController.GetPathTraversalTween(edge.From, edge.To);
                        if (pathTween.isAlive)
                        {
                            _revealSequence.Chain(pathTween);
                        }
                    }
                }
            }
        }

        #endregion Animation
    }
}