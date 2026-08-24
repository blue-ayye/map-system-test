using PrimeTween;
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
        [SerializeField] private MapDataHandler _mapDataHandler;
        [SerializeField] private int _playerInputSeed = 0;
        [SerializeField] private bool _usePlayerInputSeed = false;
        [SerializeField] private int _generationAttempts = 1;

        [Header("Animation Settings")]
        [SerializeField] private float _nodeSpawnDuration = 0;
        [SerializeField] private float _pathDrawDuration = 0;

        private MapNode[,] _mapGrid;
        private bool _isCustomSeedUsed;
        private Sequence _revealSequence;

        [field: SerializeField] private int GeneratedSeed { get; set; } // Set it to private later

        [ContextMenu("Generate New Map")]
        private void Start()
        {
            _mapGridGenerator.CalculateBounds();

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

            AnimateMapReveal();
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
            GenerateMap();

            // Reassign node types based on loaded data just to be sure
            _mapNodeTypeAssigner.ReadFrom(mapData);

            GenerateMapVisuals();

            _mapTraversalController.Initialize(_mapGrid, _mapPathGenerator.PathViews);
            _mapTraversalController.ReadFrom(mapData);

            AnimateMapReveal();
        }

        private void WriteTo(MapData mapData)
        {
            mapData.Seed = GeneratedSeed;
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
            GeneratedSeed = mapData.Seed;
        }
    }
}