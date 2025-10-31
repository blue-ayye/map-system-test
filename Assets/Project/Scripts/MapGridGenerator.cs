using UnityEngine;

namespace BP.MapSystem
{
    public class MapGridGenerator : MonoBehaviour
    {
        [SerializeField] private int _maxLevels = 9;
        [SerializeField] private int _nodesPerLevel = 7;
        [SerializeField] private Transform _nodeViewParent;
        [SerializeField] private Transform _mapAreaBoundsDefiner;
        [SerializeField] private MapNodeView _nodeViewPrefab;

        public MapNode[,] MapGrid;
        public int MaxLevels => _maxLevels;
        public int NodesPerLevel => _nodesPerLevel;
        private Vector3 _right;
        private Vector3 _up;
        private Vector3 _origin;
        private Vector3 _center;
        private Vector3 _size;

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (_mapAreaBoundsDefiner == null) return;
            CalculateBounds();
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(_origin, _origin + _right * 2f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_origin + _up * 2f, _origin + _right * 2f + _up * 2f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_origin, _origin + _up * 2f);
            Gizmos.DrawLine(_origin + _right * 2f, _origin + _right * 2f + _up * 2f);

            // Draw node positions
            // Adjust the radius according to how big the area is in world space
            float areaDiagonal = _size.magnitude;
            float radius = areaDiagonal * 0.01f;
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    float xNorm = _nodesPerLevel == 1 ? 0.5f : (float)nodeIndex / (_nodesPerLevel - 1);
                    float yNorm = _maxLevels == 1 ? 0.5f : (float)level / (_maxLevels - 1);
                    Vector3 position = _origin + _right * 2f * xNorm + _up * 2f * yNorm;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(position, radius);
                }
            }

            // Draw level and node indices
            UnityEditor.Handles.color = Color.white;
            for (int index = 0; index < _nodesPerLevel; index++)
            {
                int level = 0;
                float xNorm = _nodesPerLevel == 1 ? 0.5f : (float)index / (_nodesPerLevel - 1);
                float yNorm = _maxLevels == 1 ? 0.5f : (float)level / (_maxLevels - 1);
                Vector3 position = _origin + _right * 2f * xNorm + _up * 2f * yNorm;
                UnityEditor.Handles.Label(position + Vector3.up * radius * 1.5f, $"     N{index}");
            }

            for (int index = 0; index < _maxLevels; index++)
            {
                int nodeIndex = 0;
                float xNorm = _nodesPerLevel == 1 ? 0.5f : (float)nodeIndex / (_nodesPerLevel - 1);
                float yNorm = _maxLevels == 1 ? 0.5f : (float)index / (_maxLevels - 1);
                Vector3 position = _origin + _right * 2f * xNorm + _up * 2f * yNorm;
                UnityEditor.Handles.Label(position + Vector3.up * radius * 1.5f, $"L{index}     ");
            }
        }

#endif

        /// <summary>
        /// This is just for testing purposes. Call the individual methods from a control script to control the flow.
        /// </summary>
        ///
        [ContextMenu("Create Map Grid")]
        public void CreateMapGrid()
        {
            ClearNodeViews();

            CreateNodeGrid();
            CreateNodeViews();
        }

        /// <summary>
        /// Detects whether we're in UI or 3D mode and calculates world-space bounds accordingly.
        /// </summary>
        ///
        private void CalculateBounds()
        {
            if (_mapAreaBoundsDefiner == null)
            {
                Debug.LogError("Map Area Bounds Definer is not assigned.");
                return;
            }

            if (_mapAreaBoundsDefiner.TryGetComponent(out RectTransform rt))
            {
                _size = Vector3.Scale(rt.rect.size, rt.lossyScale);
                _center = rt.TransformPoint(rt.rect.center);
                _right = rt.right * (_size.x * 0.5f);
                _up = rt.up * (_size.y * 0.5f);
                _origin = _center - _right - _up;
            }
            else if (_mapAreaBoundsDefiner.TryGetComponent(out BoxCollider collider))
            {
                _size = Vector3.Scale(collider.size, _mapAreaBoundsDefiner.lossyScale);
                _center = _mapAreaBoundsDefiner.TransformPoint(collider.center);
                _right = _mapAreaBoundsDefiner.right * (_size.x * 0.5f);
                _up = _mapAreaBoundsDefiner.up * (_size.y * 0.5f);
                _origin = _center - _right - _up;
            }
            else
            {
                Debug.LogError("Map Area Bounds Definer must have either a RectTransform or BoxCollider component.");
            }
        }

        public void ClearUnusedNodes()
        {
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = MapGrid[level, nodeIndex];
                    if (node.ParentNodes.Count == 0 && node.ChildNodes.Count == 0)
                    {
                        MapGrid[level, nodeIndex] = null;
                    }
                }
            }
        }

        public void ClearNodeViews()
        {
            foreach (Transform child in _nodeViewParent)
            {
                Destroy(child.gameObject);
            }
        }

        public void CreateNodeViews()
        {
            if (MapGrid == null) return;

            CalculateBounds();

            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = MapGrid[level, nodeIndex];
                    if (node == null) continue;

                    float xNorm = _nodesPerLevel == 1 ? 0.5f : (float)nodeIndex / (_nodesPerLevel - 1);
                    float yNorm = _maxLevels == 1 ? 0.5f : (float)level / (_maxLevels - 1);

                    Vector3 position = _origin + _right * 2f * xNorm + _up * 2f * yNorm;

                    var nodeView = Instantiate(_nodeViewPrefab, _nodeViewParent);
                    nodeView.transform.localPosition = position;
                    nodeView.transform.rotation = _mapAreaBoundsDefiner.rotation;
                    node.NodeView = nodeView;
                }
            }
        }

        public void CreateNodeGrid()
        {
            MapGrid = new MapNode[_maxLevels, _nodesPerLevel];
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = new MapNode(level, nodeIndex);
                    MapGrid[level, nodeIndex] = node;
                }
            }
        }
    }
}