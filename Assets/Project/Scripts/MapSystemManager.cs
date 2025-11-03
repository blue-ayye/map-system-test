using UnityEngine;

namespace BP.MapSystem
{
    public class MapSystemManager : MonoBehaviour
    {
        [SerializeField] private MapGridGenerator _mapGridGenerator;
        [SerializeField] private MapPathGenerator _mapPathGenerator;
        [SerializeField] private MapNodeTypeAssigner _mapNodeTypeAssigner;
        [SerializeField] private int _playerInputSeed = 0;
        [SerializeField] private bool _usePlayerInputSeed = false;

        [field: SerializeField] private int GeneratedSeed { get; set; } // Set it to private later

        [ContextMenu("Generate New Map")]
        private void Start()
        {
            _mapGridGenerator.ClearNodeViews();
            _mapPathGenerator.ClearPathViews();

            GenerateSeed();

            var mapJitterRNG = new System.Random(GeneratedSeed);

            _mapGridGenerator.Initialize(mapJitterRNG);
            _mapGridGenerator.CreateNodeGrid();

            var mapPathingRNG = new System.Random(GeneratedSeed + 1);
            int maxLevels = _mapGridGenerator.MaxLevels;
            int nodesPerLevel = _mapGridGenerator.NodesPerLevel;
            var mapGrid = _mapGridGenerator.MapGrid;
            _mapPathGenerator.Initialize(mapGrid, maxLevels, nodesPerLevel, mapPathingRNG);
            _mapPathGenerator.SelectStartingNodes();
            _mapPathGenerator.GeneratePaths();

            _mapGridGenerator.ClearUnusedNodes();

            _mapGridGenerator.CreateNodeViews();
            _mapPathGenerator.CreatePathViews();

            var mapNodeTypeRNG = new System.Random(GeneratedSeed + 2);
            _mapNodeTypeAssigner.AssignNodeTypes(mapGrid, mapNodeTypeRNG);
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