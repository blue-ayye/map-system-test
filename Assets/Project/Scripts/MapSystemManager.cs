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

        [field: SerializeField] private int GeneratedSeed { get; set; } // Set it to private later

        [ContextMenu("Generate New Map")]
        private void Start()
        {
            // 1. Create seed
            GenerateSeed();

            // 2. Create map node data
            var mapJitterRNG = new System.Random(GeneratedSeed);
            _mapGridGenerator.Initialize(mapJitterRNG);
            var mapGrid = _mapGridGenerator.CreateNodeGrid();

            // 3. Create map path data
            var mapPathingRNG = new System.Random(GeneratedSeed + 1);
            _mapPathGenerator.Initialize(mapGrid, mapPathingRNG);
            _mapPathGenerator.SelectStartingNodes();
            _mapPathGenerator.GeneratePaths();
            _mapGridGenerator.ClearUnusedNodes();

            // 4. Assign node types to map node data
            var mapNodeTypeRNG = new System.Random(GeneratedSeed + 2);
            _mapNodeTypeAssigner.Initialize(mapNodeTypeRNG);
            _mapNodeTypeAssigner.AssignNodeTypes(mapGrid);

            // 5. Create map node and path views
            _mapGridGenerator.ClearNodeViews();
            _mapPathGenerator.ClearPathViews();

            _mapGridGenerator.CreateNodeViews();
            _mapPathGenerator.CreatePathViews();
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