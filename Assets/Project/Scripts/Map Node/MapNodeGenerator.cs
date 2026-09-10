using PrimeTween;
using System;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapNodeGenerator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The prefab instantiated for each valid node in the map.")]
        [SerializeField] private Transform _nodeViewPrefab;
        [Tooltip("The parent transform to hold all instantiated node views.")]
        [SerializeField] private Transform _nodeViewParent;
        [Tooltip("A RectTransform or BoxCollider defining the physical bounds of the map.")]
        [SerializeField] private Transform _mapAreaBoundsDefiner;
        [Tooltip("The designated node type for the very first starting node.")]
        [UnityEngine.Serialization.FormerlySerializedAs("_intialNodeType")]
        [SerializeField] private MapNodeTypeSO _initialNodeType;
        [Tooltip("The designated node type for the final boss/exit node.")]
        [SerializeField] private MapNodeTypeSO _finalNodeType;

        [Header("Scale Multipliers")]
        [Tooltip("Scale multiplier applied to standard grid nodes.")]
        [SerializeField] private float _nodeScaleMultiplier = 1f;
        [Tooltip("Scale multiplier applied exclusively to the initial starting node.")]
        [SerializeField] private float _initialNodeScaleMultiplier = 1.5f;
        [Tooltip("Scale multiplier applied exclusively to the final ending node.")]
        [SerializeField] private float _finalNodeScaleMultiplier = 1.5f;

        [Header("Grid Dimensions")]
        [Tooltip("Total number of horizontal rows (levels) in the map.")]
        [SerializeField] private int _maxLevels = 9;
        [Tooltip("Total number of nodes per horizontal level.")]
        [SerializeField] private int _nodesPerLevel = 7;

        [Header("Layout & Orientation")]
        [Tooltip("The flow direction of the map generation (e.g., BottomToTop for Slay the Spire style).")]
        [SerializeField] private MapDirection _direction = MapDirection.TopToBottom;
        [Tooltip("Z-axis rotation applied to every instantiated node view.")]
        [SerializeField] private int _zRotation;

        [Header("Positioning & Jitter")]
        [Tooltip("Extra spatial offset for the initial node away from the main grid.")]
        [SerializeField] private float _initialNodeDistance = 2f;
        [Tooltip("Extra spatial offset for the final node away from the main grid.")]
        [SerializeField] private float _finalNodeDistance = 2f;
        [Tooltip("If true, offsets nodes slightly from their perfect mathematical grid positions.")]
        [SerializeField] private bool _applyJitter = true;
        [Tooltip("Maximum horizontal position variance as a percentage of spacing.")]
        [SerializeField, Range(0f, 50f)] private float _nodeSpaceJitterPercentage;
        [Tooltip("Maximum vertical position variance as a percentage of spacing.")]
        [SerializeField, Range(0f, 50f)] private float _levelSpaceJitterPercentage;

        [Header("Animation")]
        [Tooltip("Duration of the node popping in when revealed.")]
        [SerializeField, Min(0.0001f)] private float _nodeSpawnAnimationDuration = 0.3f;

        private MapNode[,] _mapGrid;
        private MapBoundsData _bounds;
        private Vector2 _dynamicSpacing;
        private System.Random _jitterRNG;

        private const string _missingBoundsDefinerError = "Map Area Bounds Definer is not assigned.";
        private const string _missingBoundsComponentError = "Map Area Bounds Definer must have RectTransform or BoxCollider component.";
        private const string _missingNodeViewInterfaceError = "Node View Prefab does not have a component that implements IMapNodeView.";

        public MapNode InitialNode { get; private set; }
        public MapNode FinalNode { get; private set; }

        public int MaxLevels { get => _maxLevels; set => _maxLevels = value; }
        public int NodesPerLevel { get => _nodesPerLevel; set => _nodesPerLevel = value; }
        public MapDirection Direction { get => _direction; set => _direction = value; }
        public int NodeFacingDirection { get => _zRotation; set => _zRotation = value; }
        public float NodeSpawnAnimationDuration { get => _nodeSpawnAnimationDuration; set => _nodeSpawnAnimationDuration = value; }

        #region Initialization

        public void Initialize(System.Random jitterRNG)
        {
            _jitterRNG = jitterRNG;
        }

        #endregion Initialization

        #region Grid Generation

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
                boundData.Size = Vector3.Scale(rt.rect.size, rt.lossyScale);
                boundData.Center = rt.TransformPoint(rt.rect.center);
            }
            else if (_mapAreaBoundsDefiner.TryGetComponent(out BoxCollider collider))
            {
                boundData.Size = Vector3.Scale(collider.size, _mapAreaBoundsDefiner.lossyScale);
                boundData.Center = _mapAreaBoundsDefiner.TransformPoint(collider.center);
            }
            else
            {
                Debug.LogError(_missingBoundsComponentError);
                return;
            }

            boundData.Right = _mapAreaBoundsDefiner.right * (boundData.Size.x * 0.5f);
            boundData.Up = _mapAreaBoundsDefiner.up * (boundData.Size.y * 0.5f);

            switch (_direction)
            {
                case MapDirection.TopToBottom:
                    boundData.Origin = boundData.Center - boundData.Right + boundData.Up;
                    boundData.Up = -boundData.Up;
                    break;

                case MapDirection.BottomToTop:
                    boundData.Origin = boundData.Center + boundData.Right - boundData.Up;
                    boundData.Right = -boundData.Right;
                    break;

                case MapDirection.LeftToRight:
                    (boundData.Right, boundData.Up) = (boundData.Up, boundData.Right);
                    boundData.Origin = boundData.Center - boundData.Right - boundData.Up;
                    break;

                case MapDirection.RightToLeft:
                    (boundData.Right, boundData.Up) = (-boundData.Up, -boundData.Right);
                    boundData.Origin = boundData.Center - boundData.Right - boundData.Up;
                    break;
            }

            _bounds = boundData;
            _dynamicSpacing = new Vector2(
                _nodesPerLevel > 1 ? 1f / (_nodesPerLevel - 1) : 0.5f,
                _maxLevels > 1 ? 1f / (_maxLevels - 1) : 0.5f
            );
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
            if (_initialNodeType != null)
            {
                int centerIndex = _nodesPerLevel / 2;
                int randomIndex = 0;
                if (_nodesPerLevel > 1)
                    randomIndex = Mathf.Clamp(centerIndex + _jitterRNG.Next(-1, 2), 0, _nodesPerLevel - 1);

                InitialNode = new MapNode(-1, randomIndex)
                {
                    NodeType = _initialNodeType,
                };

                Vector3 pos = GetNodePosition(0, randomIndex, _applyJitter);
                pos -= _bounds.Up.normalized * _initialNodeDistance;
                InitialNode.Position = pos;
            }

            FinalNode = null;
            if (_finalNodeType != null)
            {
                int centerIndex = _nodesPerLevel / 2;
                int randomIndex = 0;
                if (_nodesPerLevel > 1)
                    randomIndex = Mathf.Clamp(centerIndex + _jitterRNG.Next(-1, 2), 0, _nodesPerLevel - 1);

                FinalNode = new MapNode(_maxLevels, randomIndex)
                {
                    NodeType = _finalNodeType,
                };

                Vector3 pos = GetNodePosition(_maxLevels - 1, randomIndex, _applyJitter);
                pos += _bounds.Up.normalized * _finalNodeDistance;
                FinalNode.Position = pos;
            }

            return _mapGrid;
        }

        private Vector3 GetNodePosition(int level, int nodeIndex, bool applyJitter = false)
        {
            float xNorm = _dynamicSpacing.x * nodeIndex;
            float yNorm = _dynamicSpacing.y * level;

            if (!applyJitter)
            {
                return _bounds.Origin
                    + _bounds.Right * xNorm * 2f
                    + _bounds.Up * yNorm * 2f;
            }
            else
            {
                float nodeJitterFactor = _nodeSpaceJitterPercentage * 0.01f;
                float levelJitterFactor = _levelSpaceJitterPercentage * 0.01f;

                float jitterX = (float)(_jitterRNG.NextDouble() * 2f - 1f) * _dynamicSpacing.x * nodeJitterFactor;
                float jitterY = (float)(_jitterRNG.NextDouble() * 2f - 1f) * _dynamicSpacing.y * levelJitterFactor;

                return _bounds.Origin
                    + _bounds.Right * (xNorm + jitterX) * 2f
                    + _bounds.Up * (yNorm + jitterY) * 2f;
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

        #endregion Grid Generation

        #region View Management

        public void CreateNodeViews()
        {
            if (_mapGrid == null) return;

            Quaternion rotation = Quaternion.Euler(
                _mapAreaBoundsDefiner.rotation.eulerAngles.x,
                _mapAreaBoundsDefiner.rotation.eulerAngles.y,
                _zRotation
            );

            if (InitialNode != null) CreateSingleNodeView(InitialNode, rotation, _initialNodeScaleMultiplier);
            if (FinalNode != null) CreateSingleNodeView(FinalNode, rotation, _finalNodeScaleMultiplier);

            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = _mapGrid[level, nodeIndex];
                    if (node == null) continue;

                    CreateSingleNodeView(node, rotation, _nodeScaleMultiplier);
                }
            }
        }

        private void CreateSingleNodeView(MapNode node, Quaternion rotation, float nodeScaleMultiplier)
        {
            Transform nodeViewTransform = Instantiate(_nodeViewPrefab, _nodeViewParent);
            nodeViewTransform.SetPositionAndRotation(node.Position, rotation);
            nodeViewTransform.localScale *= nodeScaleMultiplier;

            if (nodeViewTransform.TryGetComponent(out IMapNodeView nodeView))
            {
                node.NodeView = nodeView;
                node.Scale = nodeViewTransform.localScale;
                nodeView.Initialize(node);
            }
            else
            {
                Debug.LogError(_missingNodeViewInterfaceError);
            }
        }

        public void ClearNodeViews()
        {
            foreach (Transform child in _nodeViewParent)
            {
                Destroy(child.gameObject);
            }
        }

        #endregion View Management

        #region Animation

        public void AppendNodeSpawnToSequence(ref Sequence sequence, MapNode node)
        {
            if (node?.NodeView != null)
            {
                sequence.Chain(node.NodeView.AnimateSpawn(_nodeSpawnAnimationDuration));
            }
        }

        public void AppendLevelNodesToSequence(ref Sequence sequence, int level)
        {
            bool firstChained = false;
            for (int index = 0; index < _nodesPerLevel; index++)
            {
                var node = _mapGrid[level, index];
                if (node?.NodeView != null)
                {
                    Tween nodeTween = node.NodeView.AnimateSpawn(_nodeSpawnAnimationDuration);

                    if (!firstChained)
                    {
                        sequence.Chain(nodeTween);
                        firstChained = true;
                    }
                    else
                    {
                        sequence.Group(nodeTween);
                    }
                }
            }
        }

        #endregion Animation

        #region Unity Editor

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (_mapAreaBoundsDefiner == null) return;

            CalculateBounds();
            float areaDiagonal = _bounds.Size.magnitude;
            float radius = areaDiagonal * 0.01f;

            // Base gizmos
            Gizmos.color = Color.blue; // Start point
            Gizmos.DrawLine(_bounds.Origin, _bounds.Origin + _bounds.Right * 2f);

            Gizmos.color = Color.red; // End point
            Gizmos.DrawLine(_bounds.Origin + _bounds.Up * 2f, _bounds.Origin + _bounds.Right * 2f + _bounds.Up * 2f);

            Gizmos.color = Color.green; // Sides
            Gizmos.DrawLine(_bounds.Origin, _bounds.Origin + _bounds.Up * 2f);
            Gizmos.DrawLine(_bounds.Origin + _bounds.Right * 2f, _bounds.Origin + _bounds.Right * 2f + _bounds.Up * 2f);

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
            if (_initialNodeType != null)
            {
                int centerIndex = _nodesPerLevel / 2;
                int randomIndex = 0;
                if (_nodesPerLevel > 1)
                    randomIndex = Mathf.Clamp(centerIndex + tempRandom.Next(-1, 2), 0, _nodesPerLevel - 1);

                Vector3 pos = GetNodePosition(0, randomIndex);
                pos -= _bounds.Up.normalized * _initialNodeDistance;
                Gizmos.DrawSphere(pos, radius * _initialNodeScaleMultiplier);

                Gizmos.color = Color.white;
                for (int i = 0; i < _nodesPerLevel; i++)
                {
                    Vector3 level0Pos = GetNodePosition(0, i);
                    Gizmos.DrawLine(pos, level0Pos);
                }

                UnityEditor.Handles.color = Color.cyan;
                pos.x -= radius * _initialNodeScaleMultiplier * 3f;
                pos.y += radius * _initialNodeScaleMultiplier * 2f;
                UnityEditor.Handles.Label(pos, "Initial Node");
            }

            Gizmos.color = Color.red;
            if (_finalNodeType != null)
            {
                int centerIndex = _nodesPerLevel / 2;
                int randomIndex = 0;
                if (_nodesPerLevel > 1)
                    randomIndex = Mathf.Clamp(centerIndex + tempRandom.Next(-1, 2), 0, _nodesPerLevel - 1);

                Vector3 pos = GetNodePosition(_maxLevels - 1, randomIndex);
                pos += _bounds.Up.normalized * _finalNodeDistance;
                Gizmos.DrawSphere(pos, radius * _finalNodeScaleMultiplier);

                Gizmos.color = Color.white;
                for (int i = 0; i < _nodesPerLevel; i++)
                {
                    Vector3 lastLevelPos = GetNodePosition(_maxLevels - 1, i);
                    Gizmos.DrawLine(lastLevelPos, pos);
                }

                UnityEditor.Handles.color = Color.cyan;
                pos.x -= radius * _finalNodeScaleMultiplier * 1f;
                pos.y += radius * _finalNodeScaleMultiplier * 2f;
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

        #endregion Unity Editor
    }
}