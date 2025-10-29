using System.Collections.Generic;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapSystemManager : MonoBehaviour
    {
        [SerializeField] private MapGridGenerator _mapGridGenerator;
        [SerializeField] private MapPathGenerator _mapPathGenerator;
        [SerializeField] private int _playerInputSeed = 0;
        [SerializeField] private bool _usePlayerInputSeed = false;

        [field: SerializeField] private int GeneratedSeed { get; set; } // Set it to private later

        [ContextMenu("Generate New Map")]
        private void Start()
        {
            _mapGridGenerator.ClearNodeViews();
            _mapPathGenerator.ClearPathViews();

            _mapGridGenerator.CreateNodeGrid();

            int? seed = _usePlayerInputSeed ? _playerInputSeed : null;
            var pRNG = InitializePRNG(seed);
            int maxLevels = _mapGridGenerator.MaxLevels;
            int nodesPerLevel = _mapGridGenerator.NodesPerLevel;
            var mapGrid = _mapGridGenerator.MapGrid;

            _mapPathGenerator.Initialize(mapGrid, maxLevels, nodesPerLevel, pRNG);
            _mapPathGenerator.SelectStartingNodes();
            _mapPathGenerator.GeneratePaths();

            _mapGridGenerator.ClearUnusedNodes();

            _mapGridGenerator.CreateNodeViews();
            _mapPathGenerator.CreatePathViews();

            AssignNodeTypes(mapGrid, pRNG);
        }

        private System.Random InitializePRNG(int? seed)
        {
            if (seed == null)
            {
                int intMinMaxSeed = Random.Range(int.MinValue, int.MaxValue);
                int dateTimeSeed = System.DateTime.Now.Millisecond;
                GeneratedSeed = Mathf.Abs(intMinMaxSeed + dateTimeSeed);
            }
            else
            {
                GeneratedSeed = Mathf.Abs(seed.Value);
            }

            return new System.Random(GeneratedSeed);
        }

        [SerializeField] private List<MapNodeTypeSO> _nodeTypes;

        private void AssignNodeTypes(MapNode[,] mapGrid, System.Random pRNG)
        {
            int maxLevels = mapGrid.GetLength(0);
            int nodesPerLevel = mapGrid.GetLength(1);
            for (int level = 0; level < maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < nodesPerLevel; nodeIndex++)
                {
                    var node = mapGrid[level, nodeIndex];
                    if (node != null)
                    {
                        int typeIndex = pRNG.Next(_nodeTypes.Count);
                        var nodeType = _nodeTypes[typeIndex];
                        var nodeView = node.NodeView;
                        if (nodeView != null)
                        {
                            nodeView.SetNodeType(nodeType);
                        }
                    }
                }
            }
        }
    }
}