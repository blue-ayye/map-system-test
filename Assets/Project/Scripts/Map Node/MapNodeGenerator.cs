using System;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapNodeGenerator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _nodeViewPrefab;
        [SerializeField] private Transform _nodeViewParent;
        [SerializeField] private Transform _mapAreaBoundsDefiner;

        [Header("Special Node Settings")]
        [SerializeField] private MapNodeTypeSO _intialNodeType;
        [SerializeField] private MapNodeTypeSO _finalNodeType;
        [SerializeField] private float _intialNodeSizeMultiplier = 1.5f;
        [SerializeField] private float _finalNodeSizeMultiplier = 1.5f;
        [SerializeField] private float _initialNodeDistanceMultiplier = 1.5f;
        [SerializeField] private float _finalNodeDistanceMultiplier = 1.5f;

        [Header("Map Grid Settings")]
        [SerializeField] private int _maxLevels = 9;
        [SerializeField] private int _nodesPerLevel = 7;

        [Header("Rotation and Direction")]
        [SerializeField] private MapDirection _direction = MapDirection.TopToBottom;
        [SerializeField] private int _zRotation;

        [Header("Jitter Settings")]
        [SerializeField] private bool _applyJitter = true;
        [SerializeField, Range(0f, 50f)] private float _nodeSpaceJitterPercentage;
        [SerializeField, Range(0f, 50f)] private float _levelSpaceJitterPercentage;

        private MapNode[,] _mapGrid;
        private MapBoundsData _bounds;
        private Vector2 _dynamicSpacing;
        private System.Random _jitterRNG;

        private const string _missingBoundsDefinerError = "Map Area Bounds Definer is not assigned.";
        private const string _missingBoundsComponentError = "Map Area Bounds Definer must have RectTransform or BoxCollider component.";
        private const string _missingNodeViewInterfaceError = "Node View Prefab does not have a component that implements IMapNodeView.";

        public MapNode InitialNode { get; private set; }
        public MapNode FinalNode { get; private set; }

        #region Public APIs

        public void Initialize(System.Random jitterRNG)
        {
            _jitterRNG = jitterRNG;
        }

        public MapNode[,] CreateNodeGrid()
        {
            _mapGrid = new MapNode[_maxLevels, _nodesPerLevel];

            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = new MapNode(level, nodeIndex)
                    {
                        Position = GetNodePosition(level, nodeIndex, _applyJitter)
                    };

                    _mapGrid[level, nodeIndex] = node;
                }
            }

            InitialNode = null;
            if (_intialNodeType != null)
            {
                int centerIndex = _nodesPerLevel / 2;
                int randomIndex = Mathf.Clamp(centerIndex + _jitterRNG.Next(-1, 2), 0, _nodesPerLevel - 1);
                InitialNode = new MapNode(-1, randomIndex)
                {
                    NodeType = _intialNodeType,
                };

                Vector3 pos = GetNodePosition(-1, randomIndex, _applyJitter);
                pos -= (_bounds.up * _dynamicSpacing.y * _initialNodeDistanceMultiplier * 2f);
                InitialNode.Position = pos;
            }

            FinalNode = null;
            if (_finalNodeType != null)
            {
                int centerIndex = _nodesPerLevel / 2;
                int randomIndex = Mathf.Clamp(centerIndex + _jitterRNG.Next(-1, 2), 0, _nodesPerLevel - 1);
                FinalNode = new MapNode(_maxLevels, randomIndex)
                {
                    NodeType = _finalNodeType,
                };

                Vector3 pos = GetNodePosition(_maxLevels, randomIndex, _applyJitter);
                pos += (_bounds.up * _dynamicSpacing.y * _finalNodeDistanceMultiplier * 2f);
                FinalNode.Position = pos;
            }

            return _mapGrid;
        }

        public void CreateNodeViews()
        {
            if (_mapGrid == null) return;

            Quaternion rotation = Quaternion.Euler(
                _mapAreaBoundsDefiner.rotation.eulerAngles.x,
                _mapAreaBoundsDefiner.rotation.eulerAngles.y,
                _zRotation
            );

            if (InitialNode != null) CreateSingleNodeView(InitialNode, rotation);
            if (FinalNode != null) CreateSingleNodeView(FinalNode, rotation);

            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = _mapGrid[level, nodeIndex];
                    if (node == null) continue;

                    CreateSingleNodeView(node, rotation);
                }
            }
        }

        public void ClearUnusedNodes()
        {
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = _mapGrid[level, nodeIndex];
                    if (node != null && node.ParentNodes.Count == 0 && node.ChildNodes.Count == 0)
                    {
                        _mapGrid[level, nodeIndex] = null;
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

        public void CalculateBounds()
        {
            if (_mapAreaBoundsDefiner == null)
            {
                Debug.LogError(_missingBoundsDefinerError);
                return;
            }

            MapBoundsData boundData = new MapBoundsData();

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
                Debug.LogError(_missingBoundsComponentError);
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
                    (boundData.right, boundData.up) = (boundData.up, boundData.right);
                    boundData.origin = boundData.center - boundData.right - boundData.up;
                    break;
                case MapDirection.RightToLeft:
                    (boundData.right, boundData.up) = (-boundData.up, -boundData.right);
                    boundData.origin = boundData.center - boundData.right - boundData.up;
                    break;
            }

            _bounds = boundData;
            _dynamicSpacing = new Vector2(
                _nodesPerLevel > 1 ? 1f / (_nodesPerLevel - 1) : 0.5f,
                _maxLevels > 1 ? 1f / (_maxLevels - 1) : 0.5f
            );
        }

        #endregion Public APIs

        #region Helpers

        private void CreateSingleNodeView(MapNode node, Quaternion rotation)
        {
            Transform nodeViewTransform = Instantiate(_nodeViewPrefab, _nodeViewParent);
            nodeViewTransform.SetPositionAndRotation(node.Position, rotation);

            if (nodeViewTransform.TryGetComponent(out IMapNodeView nodeView))
            {
                nodeView.Initialize(node);
                node.NodeView = nodeView;
            }
            else
            {
                Debug.LogError(_missingNodeViewInterfaceError);
            }
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
                float nodeJitterFactor = _nodeSpaceJitterPercentage * 0.01f;
                float levelJitterFactor = _levelSpaceJitterPercentage * 0.01f;

                float jitterX = (float)(_jitterRNG.NextDouble() * 2f - 1f) * _dynamicSpacing.x * nodeJitterFactor;
                float jitterY = (float)(_jitterRNG.NextDouble() * 2f - 1f) * _dynamicSpacing.y * levelJitterFactor;

                return _bounds.origin
                    + _bounds.right * (xNorm + jitterX) * 2f
                    + _bounds.up * (yNorm + jitterY) * 2f;
            }
        }

        #endregion Helpers

        #region Unity Editor & Debugging

#if UNITY_EDITOR

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

            var tempRandom = new System.Random((int)Time.time);

            // Draw special nodes if they exist
            Gizmos.color = Color.green;
            if (_intialNodeType != null)
            {
                var centerIndex = _nodesPerLevel / 2;
                var randomIndex = Mathf.Clamp(centerIndex + tempRandom.Next(-1, 2), 0, _nodesPerLevel - 1);
                Vector3 pos = GetNodePosition(-1, randomIndex);
                pos -= (_bounds.up * _dynamicSpacing.y * _initialNodeDistanceMultiplier * 2f);
                Gizmos.DrawSphere(pos, radius * _intialNodeSizeMultiplier);

                Gizmos.color = Color.white;
                for (int i = 0; i < _nodesPerLevel; i++)
                {
                    Vector3 level0Pos = GetNodePosition(0, i);
                    Gizmos.DrawLine(pos, level0Pos);
                }

                UnityEditor.Handles.color = Color.cyan;
                pos.x -= radius * _intialNodeSizeMultiplier * 3f;
                pos.y += radius * _intialNodeSizeMultiplier * 2f;
                UnityEditor.Handles.Label(pos, "Initial Node");
            }

            Gizmos.color = Color.red;
            if (_finalNodeType != null)
            {
                var centerIndex = _nodesPerLevel / 2;
                var randomIndex = Mathf.Clamp(centerIndex + tempRandom.Next(-1, 2), 0, _nodesPerLevel - 1);
                Vector3 pos = GetNodePosition(_maxLevels, randomIndex);
                pos += (_bounds.up * _dynamicSpacing.y * _finalNodeDistanceMultiplier * 2f);
                Gizmos.DrawSphere(pos, radius * _finalNodeSizeMultiplier);

                Gizmos.color = Color.white;
                for (int i = 0; i < _nodesPerLevel; i++)
                {
                    Vector3 lastLevelPos = GetNodePosition(_maxLevels - 1, i);
                    Gizmos.DrawLine(lastLevelPos, pos);
                }

                UnityEditor.Handles.color = Color.cyan;
                pos.x -= radius * _finalNodeSizeMultiplier * 1f;
                pos.y += radius * _finalNodeSizeMultiplier * 2f;
                UnityEditor.Handles.Label(pos, "Final Node");
            }

            // Draw index labels
            UnityEditor.Handles.color = Color.cyan;
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
        }

        [ContextMenu("Debug Create Map Grid")]
        private void DebugCreateMapGrid()
        {
            ClearNodeViews();
            CreateNodeGrid();
            CreateNodeViews();
        }

#endif

        #endregion Unity Editor & Debugging
    }
}