using System.Collections.Generic;
using UnityEngine;

namespace BP.MapGeneration
{
    [System.Serializable]
    public class MapNode
    {
        public int Level;
        public int NodeIndex;

        public MapNode(int level, int nodeIndex)
        {
            Level = level;
            NodeIndex = nodeIndex;
        }
    }

    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private int MaxLevels = 5;
        [SerializeField] private int NodesPerLevel = 8;
        [SerializeField] private int _playerInputSeed = 12345;
        [SerializeField] private bool _usePlayerInputSeed = false;
        [SerializeField] private int _uniquePaths = 3;
        [SerializeField] private int _totalPaths = 7;

        private MapNode[,] _mapGrid;
        private System.Random _pRNG;
        public int GeneratedSeed { get; private set; }
        [SerializeField] private List<MapNode> _startingNodes = new List<MapNode>();

        private void OnDrawGizmos()
        {
            if (_mapGrid == null)
                return;
            for (int level = 0; level < MaxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < NodesPerLevel; nodeIndex++)
                {
                    Gizmos.color = _startingNodes.Contains(_mapGrid[level, nodeIndex]) ? Color.green : Color.white;
                    Vector3 position = new Vector3(nodeIndex * 2.0f, level * 2.0f, 0);
                    Gizmos.DrawSphere(position, 0.3f);
                }
            }
        }

        private void Start()
        {
            int? seed = _usePlayerInputSeed ? _playerInputSeed : (int?)null;
            InitializePRNG(seed);
            CreateNodeGrid();
            SelectStartingNodes();
        }

        private void CreateNodeGrid()
        {
            _mapGrid = new MapNode[MaxLevels, NodesPerLevel];
            for (int level = 0; level < MaxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < NodesPerLevel; nodeIndex++)
                {
                    var node = new MapNode(level, nodeIndex);
                    _mapGrid[level, nodeIndex] = node;
                }
            }
        }

        private void InitializePRNG(int? seed)
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

            _pRNG = new System.Random(GeneratedSeed);
        }

        private void SelectStartingNodes()
        {
            int startingLevel = 0;
            _startingNodes.Clear();
            // Ensure at least <MinUniqueStartingPoints> number of nodes are unique
            for (int i = 0; i < _uniquePaths; i++)
            {
                MapNode randomNode;
                do
                {
                    int randomIndex = _pRNG.Next(0, NodesPerLevel);
                    randomNode = _mapGrid[startingLevel, randomIndex];
                } while (_startingNodes.Contains(randomNode));
                _startingNodes.Add(randomNode);
            }

            // Then fill the rest allowing duplicate nodes to allow multiple paths from same starting point
            while (_startingNodes.Count < _totalPaths)
            {
                int randomIndex = _pRNG.Next(0, NodesPerLevel);
                var randomNode = _mapGrid[startingLevel, randomIndex];
                _startingNodes.Add(randomNode);
            }
        }
    }
}