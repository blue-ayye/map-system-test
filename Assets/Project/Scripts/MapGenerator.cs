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
        public const int MaxLevels = 5;
        public const int NodesPerLevel = 8;

        private MapNode[,] _mapGrid;

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
    }
}