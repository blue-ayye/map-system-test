using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapSystemManager : MonoBehaviour
    {
        [SerializeField] private MapNodeGenerator _mapGridGenerator;
        [SerializeField] private MapPathGenerator _mapPathGenerator;
        [SerializeField] private MapNodeTypeAssigner _mapNodeTypeAssigner;
        [SerializeField] private int _playerInputSeed = 0;
        [SerializeField] private bool _usePlayerInputSeed = false;
        [SerializeField] private int _generationAttempts = 1;

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

            // If current seed has violations, pick the least violating seed from previous attempts
            if (seedViolations[GeneratedSeed] > 0)
            {
                if (!_usePlayerInputSeed)
                {
                    int bestSeed = GeneratedSeed;
                    int leastViolations = seedViolations[GeneratedSeed];
                    foreach (var kvp in seedViolations)
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
                else
                {
                    _mapNodeTypeAssigner.CheckTypeRulesValidity(true);
                    Debug.LogWarning($"You're using a custom seed {_playerInputSeed} that resulted in " +
                                     $"{seedViolations[GeneratedSeed]} rule violations. " +
                                     $"Consider using a different seed or toggle off the custom seed option" +
                                     $" to allow automatic seed generation with least violations.");
                }
            }

            GenerateMapVisuals();
        }

        private void GenerateMapVisuals()
        {
            _mapGridGenerator.ClearNodeViews();
            _mapPathGenerator.ClearPathViews();

            _mapGridGenerator.CreateNodeViews();
            _mapPathGenerator.CreatePathViews();
        }

        private void GenerateMap()
        {
            // 1. Create map node data
            var mapJitterRNG = new System.Random(GeneratedSeed);
            _mapGridGenerator.Initialize(mapJitterRNG);
            var mapGrid = _mapGridGenerator.CreateNodeGrid();

            // 2. Create map path data
            var mapPathingRNG = new System.Random(GeneratedSeed + 1);
            _mapPathGenerator.Initialize(mapGrid, mapPathingRNG);
            _mapPathGenerator.SelectStartingNodes();
            _mapPathGenerator.GeneratePaths();
            _mapGridGenerator.ClearUnusedNodes();

            // 3. Assign node types to map node data
            var mapNodeTypeRNG = new System.Random(GeneratedSeed + 2);
            _mapNodeTypeAssigner.Initialize(mapGrid, mapNodeTypeRNG);
            _mapNodeTypeAssigner.AssignNodeTypes();
        }

        private void GenerateSeed()
        {
            if (_usePlayerInputSeed)
            {
                GeneratedSeed = Mathf.Abs(_playerInputSeed);
            }
            else
            {
                int intMinMaxSeed = Random.Range(int.MinValue, int.MaxValue);
                int dateTimeSeed = System.DateTime.Now.Millisecond;
                GeneratedSeed = Mathf.Abs(intMinMaxSeed + dateTimeSeed);
            }
        }
    }
}