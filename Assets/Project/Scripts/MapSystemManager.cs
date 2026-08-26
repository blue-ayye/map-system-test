using PrimeTween;
using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapSystemManager : MonoBehaviour
    {
        #region Fields

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

        #endregion

        #region Unity Lifecycle

        private void Start() => Initialize();

        #endregion

        #region Public API

        [ContextMenu("Generate New Map")]
        public void Initialize()
        {
            _mapGridGenerator.CalculateBounds();

            int attempts = 0;
            Dictionary<int, int> seedViolations = new Dictionary<int, int>();
            do
            {
                GenerateSeed();
                RunGenerationPipeline();

                int violations = _mapNodeTypeAssigner.CountTypeRuleViolations();
                seedViolations[_generatedSeed] = violations;

                attempts++;
            } while (attempts < _generationAttempts && seedViolations[_generatedSeed] > 0 && !_usePlayerInputSeed);

            if (seedViolations[_generatedSeed] > 0)
            {
                RegenerateMapWithBestSeed(seedViolations);
            }

            GenerateMapVisuals();

            // NOTE: Initialize traversal controller after map generation and visuals creation
            // It depends on the onClick events of the node views
            _mapTraversalController.Initialize(_mapGrid, _mapPathGenerator.PathViews);

            AnimateMapReveal();
        }

        #endregion

        #region Private Helpers

        private void GenerateSeed()
        {
            if (_usePlayerInputSeed)
            {
                _generatedSeed = Mathf.Abs(_playerInputSeed);
                _isCustomSeedUsed = true;
            }
            else
            {
                int intMinMaxSeed = Random.Range(int.MinValue, int.MaxValue);
                int dateTimeSeed = System.DateTime.Now.Millisecond;
                _generatedSeed = Mathf.Abs(intMinMaxSeed + dateTimeSeed);
                _isCustomSeedUsed = false;
            }
        }

        private void RunGenerationPipeline()
        {
            // 1. Create map node data
            var mapJitterRNG = new System.Random(_generatedSeed);
            _mapGridGenerator.Initialize(mapJitterRNG);
            _mapGrid = _mapGridGenerator.CreateNodeGrid();

            // 2. Create map path data
            var mapPathingRNG = new System.Random(_generatedSeed + 1);
            _mapPathGenerator.Initialize(_mapGrid, mapPathingRNG);
            _mapPathGenerator.SelectStartingNodes();
            _mapPathGenerator.GeneratePaths();
            _mapGridGenerator.ClearUnusedNodes();

            // 3. Assign node types to map node data
            var mapNodeTypeRNG = new System.Random(_generatedSeed + 2);
            _mapNodeTypeAssigner.Initialize(_mapGrid, mapNodeTypeRNG);
            _mapNodeTypeAssigner.AssignNodeTypes();
        }

        private void RegenerateMapWithBestSeed(Dictionary<int, int> seedViolationDict)
        {
            if (_usePlayerInputSeed)
            {
                _mapNodeTypeAssigner.LogTypeRuleViolations();
                Debug.LogWarning($"You're using a custom seed {_playerInputSeed} that resulted in " +
                                 $"{seedViolationDict[_generatedSeed]} rule violations. " +
                                 $"Consider using a different seed or toggle off the custom seed option" +
                                 $" to allow automatic seed generation with least violations.");
            }
            else
            {
                int bestSeed = _generatedSeed;
                int leastViolations = seedViolationDict[_generatedSeed];
                foreach (var kvp in seedViolationDict)
                {
                    if (kvp.Value < leastViolations)
                    {
                        leastViolations = kvp.Value;
                        bestSeed = kvp.Key;
                    }
                }

                _generatedSeed = bestSeed;

                // Re-generate the map with the best seed
                RunGenerationPipeline();
                _mapNodeTypeAssigner.LogTypeRuleViolations();

                Debug.LogWarning($"Could not generate a valid map within {_generationAttempts} attempts. " +
                                 $"Using seed {_generatedSeed} with {leastViolations} rule violations.");
            }
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

        #endregion

        #region Editor / Debug

        [ContextMenu("Map Data/Save")]
        private void SaveMap()
        {
            var mapData = new MapData();
            WriteTo(mapData);
            _mapGridGenerator.WriteTo(mapData);
            _mapNodeTypeAssigner.WriteTo(mapData);
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

            // 1. Restore seed and grid parameters
            ReadFrom(mapData);
            _mapGridGenerator.ReadFrom(mapData);

            // 2. Recalculate bounds with restored parameters, then rebuild topology
            _mapGridGenerator.CalculateBounds();
            var mapJitterRNG = new System.Random(_generatedSeed);
            _mapGridGenerator.Initialize(mapJitterRNG);
            _mapGrid = _mapGridGenerator.CreateNodeGrid();

            var mapPathingRNG = new System.Random(_generatedSeed + 1);
            _mapPathGenerator.Initialize(_mapGrid, mapPathingRNG);
            _mapPathGenerator.SelectStartingNodes();
            _mapPathGenerator.GeneratePaths();
            _mapGridGenerator.ClearUnusedNodes();

            // 3. Restore node types from saved data (skip stochastic assignment)
            var mapNodeTypeRNG = new System.Random(_generatedSeed + 2);
            _mapNodeTypeAssigner.Initialize(_mapGrid, mapNodeTypeRNG);
            _mapNodeTypeAssigner.ReadFrom(mapData);

            // 4. Rebuild visuals and restore traversal state
            GenerateMapVisuals();

            _mapTraversalController.Initialize(_mapGrid, _mapPathGenerator.PathViews);
            _mapTraversalController.ReadFrom(mapData);

            AnimateMapReveal();
        }

        #endregion
    }
}
