using System.Net.NetworkInformation;
using UnityEngine;

namespace BP.MapGeneration
{
    public class MapNode
    {
        public int Level { get; private set; }
        public int NodeIndex { get; private set; }

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
        [SerializeField]private bool _usePlayerInputSeed = false;

        private MapNode[,] _mapGrid;
        private System.Random _pRNG;
        public int GeneratedSeed { get; private set; }

        private void OnDrawGizmos()
        {
            if (_mapGrid == null)
                return;
            Gizmos.color = Color.green;
            for (int level = 0; level < MaxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < NodesPerLevel; nodeIndex++)
                {
                    Vector3 position = new Vector3(nodeIndex * 2.0f, level * 2.0f,0);
                    Gizmos.DrawSphere(position, 0.3f);
                }
            }
        }

        private void Start()
        {
            CreateNodeGrid();
            int? seed = _usePlayerInputSeed ? _playerInputSeed : (int?)null;
            InitializePRNG(seed);
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
    }
}