using UnityEngine;

namespace BP.MapSystem
{
    public enum MapDirection
    {
        TopToBottom,
        BottomToTop,
        LeftToRight,
        RightToLeft
    }

    public class MapGridGenerator : MonoBehaviour
    {
        private struct BoundsData
        {
            public Vector3 origin;
            public Vector3 right;
            public Vector3 up;
            public Vector3 center;
            public Vector3 size;
        }

        [SerializeField] private int _maxLevels = 9;
        [SerializeField] private int _nodesPerLevel = 7;
        [SerializeField] private Transform _nodeViewParent;
        [SerializeField] private Transform _mapAreaBoundsDefiner;
        [SerializeField] private Transform _nodeViewPrefab;
        [SerializeField] private MapDirection _direction = MapDirection.TopToBottom;
        [SerializeField] private int _zRotation;
        [Tooltip("0.25f means up to 25% of the distance between nodes (they will never overlap)")]
        [SerializeField][Range(0f, .5f)] private float _nodeSpaceJitterAmount;
        [Tooltip("0.25f means up to 25% of the distance between levels (they will never overlap)")]
        [SerializeField][Range(0f, .5f)] private float _levelSpaceJitterAmount;
        [SerializeField] private int _jitterSeedInput = 0;
        [SerializeField] private bool _useJitterSeedInput = false;

        public MapNode[,] MapGrid;
        public int MaxLevels => _maxLevels;
        public int NodesPerLevel => _nodesPerLevel;

        private BoundsData _bounds;
        private Vector2 _dynamicSpacing;

        private void OnDrawGizmos()
        {
            if (_mapAreaBoundsDefiner == null) return;

            CalculateBounds();
            float areaDiagonal = _bounds.size.magnitude;
            float radius = areaDiagonal * 0.01f;

            // Base gizmos
            Gizmos.color = Color.blue; // Start point
            Gizmos.DrawLine(_bounds.origin, _bounds.origin + _bounds.right * 2f);
            Gizmos.color = Color.red; // End point
            Gizmos.DrawLine(_bounds.origin + _bounds.up * 2f, _bounds.origin + _bounds.right * 2f + _bounds.up * 2f);
            Gizmos.color = Color.green; // Sides
            Gizmos.DrawLine(_bounds.origin, _bounds.origin + _bounds.up * 2f);
            Gizmos.DrawLine(_bounds.origin + _bounds.right * 2f, _bounds.origin + _bounds.right * 2f + _bounds.up * 2f);

            // Draw node positions
            Gizmos.color = Color.yellow;
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    Vector3 pos = GetNodePosition(level, nodeIndex);
                    Gizmos.DrawSphere(pos, radius);
                }
            }

            // Draw index labels
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            for (int i = 0; i < _nodesPerLevel; i++)
            {
                Vector3 pos = GetNodePosition(-1, i);
                UnityEditor.Handles.Label(pos + Vector3.up * radius * 1.5f, $"N{i}");
            }

            for (int i = 0; i < _maxLevels; i++)
            {
                Vector3 pos = GetNodePosition(i, -1);
                UnityEditor.Handles.Label(pos + Vector3.up * radius * 1.5f, $"L{i}");
            }
#endif
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

                    var nodeView = Instantiate(_nodeViewPrefab, _nodeViewParent);

                    Vector3 position = GetNodePosition(level, nodeIndex, true);
                    Quaternion rotation = Quaternion.Euler(
                        _mapAreaBoundsDefiner.rotation.eulerAngles.x,
                        _mapAreaBoundsDefiner.rotation.eulerAngles.y,
                        _zRotation
                    );
                    nodeView.transform.SetPositionAndRotation(position, rotation);
                    node.NodeView = nodeView.GetComponent<IMapNodeView>();
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
                    MapGrid[level, nodeIndex] = new MapNode(level, nodeIndex);
                }
            }
        }

        public void ClearNodeViews()
        {
            foreach (Transform child in _nodeViewParent)
                Destroy(child.gameObject);
        }

        public void ClearUnusedNodes()
        {
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = MapGrid[level, nodeIndex];
                    if (node != null && node.ParentNodes.Count == 0 && node.ChildNodes.Count == 0)
                        MapGrid[level, nodeIndex] = null;
                }
            }
        }

        private void CalculateBounds()
        {
            if (_mapAreaBoundsDefiner == null)
            {
                Debug.LogError("Map Area Bounds Definer is not assigned.");
                return;
            }

            BoundsData boundData = new BoundsData();

            // Handle UI (RectTransform) or 3D (BoxCollider)
            if (_mapAreaBoundsDefiner.TryGetComponent(out RectTransform rt))
            {
                boundData.size = Vector3.Scale(rt.rect.size, rt.lossyScale);
                boundData.center = rt.TransformPoint(rt.rect.center);
            }
            else if (_mapAreaBoundsDefiner.TryGetComponent(out BoxCollider collider))
            {
                boundData.size = Vector3.Scale(collider.size, _mapAreaBoundsDefiner.lossyScale);
                boundData.center = _mapAreaBoundsDefiner.TransformPoint(collider.center);
            }
            else
            {
                Debug.LogError("Map Area Bounds Definer must have RectTransform or BoxCollider component.");
                return;
            }

            boundData.right = _mapAreaBoundsDefiner.right * (boundData.size.x * 0.5f);
            boundData.up = _mapAreaBoundsDefiner.up * (boundData.size.y * 0.5f);

            switch (_direction)
            {
                case MapDirection.TopToBottom:
                    boundData.origin = boundData.center - boundData.right + boundData.up;
                    boundData.up = -boundData.up;
                    break;

                case MapDirection.BottomToTop:
                    boundData.origin = boundData.center + boundData.right - boundData.up;
                    boundData.right = -boundData.right;
                    break;

                case MapDirection.LeftToRight:
                    (boundData.right, boundData.up) = (boundData.up, boundData.right); // Rotate 90° clockwise
                    boundData.origin = boundData.center - boundData.right - boundData.up;
                    break;

                case MapDirection.RightToLeft:
                    (boundData.right, boundData.up) = (-boundData.up, -boundData.right); // Rotate 90° counter-clockwise
                    boundData.origin = boundData.center - boundData.right - boundData.up;
                    break;
            }

            _bounds = boundData;

            // Dynamic spacing (normalized between 0 and 1)
            _dynamicSpacing = new Vector2(
                _nodesPerLevel > 1 ? 1f / (_nodesPerLevel - 1) : 0.5f,
                _maxLevels > 1 ? 1f / (_maxLevels - 1) : 0.5f
            );
        }

        private Vector3 GetNodePosition(int level, int nodeIndex, bool applyJitter = false)
        {
            float xNorm = _dynamicSpacing.x * nodeIndex;
            float yNorm = _dynamicSpacing.y * level;

            if (!applyJitter)
            {
                return _bounds.origin
                    + _bounds.right * xNorm * 2f
                    + _bounds.up * yNorm * 2f;
            }
            else
            {
                int jitterSeed = _useJitterSeedInput ? _jitterSeedInput : System.DateTime.Now.Millisecond;
                System.Random jitterRNG = new System.Random(jitterSeed + level * 73856093 + nodeIndex * 19349663);
                float jitterX = (float)(jitterRNG.NextDouble() * 2f - 1f) * _dynamicSpacing.x * _nodeSpaceJitterAmount;
                float jitterY = (float)(jitterRNG.NextDouble() * 2f - 1f) * _dynamicSpacing.y * _levelSpaceJitterAmount;

                return _bounds.origin
                    + _bounds.right * (xNorm + jitterX) * 2f
                    + _bounds.up * (yNorm + jitterY) * 2f;
            }
        }

        [ContextMenu("Debug Create Map Grid")]
        private void DebugCreateMapGrid()
        {
            ClearNodeViews();
            CreateNodeGrid();
            CreateNodeViews();
        }
    }
}