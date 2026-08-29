using System;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapNodeGenerator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Prefab for the node views.")]
        [SerializeField] private Transform _nodeViewPrefab;
        [Tooltip("Parent transform for the node views.")]
        [SerializeField] private Transform _nodeViewParent;
        [Tooltip("Use a Transform with a RectTransform or BoxCollider to define the area where the map nodes will be generated.")]
        [SerializeField] private Transform _mapAreaBoundsDefiner;

        [Header("Map Grid Settings")]
        [Tooltip("Maximum number of levels in the map grid.")]
        [SerializeField] private int _maxLevels = 9;
        [Tooltip("Maximum number of nodes per level in the map grid.")]
        [SerializeField] private int _nodesPerLevel = 7;

        [Header("Rotation and Direction")]
        [Tooltip("Direction in which the map nodes will be generated.")]
        [SerializeField] private MapDirection _direction = MapDirection.TopToBottom;
        [Tooltip("Z rotation for the node views.")]
        [SerializeField] private int _zRotation;

        [Header("Jitter Settings")]
        [Tooltip("Whether to apply jitter to node positions. If false, nodes will be evenly spaced and _nodeSpaceJitterPercentage and _levelSpaceJitterPercentage will be ignored.")]
        [SerializeField] private bool _applyJitter = true;
        [Tooltip("Jitter percentage (0-50). 25 means up to 25% of the distance between nodes.")]
        [SerializeField, Range(0f, 50f)] private float _nodeSpaceJitterPercentage;
        [Tooltip("Jitter percentage (0-50). 25 means up to 25% of the distance between levels.")]
        [SerializeField, Range(0f, 50f)] private float _levelSpaceJitterPercentage;

        private MapNode[,] _mapGrid;
        private MapBoundsData _bounds;
        private Vector2 _dynamicSpacing;
        private System.Random _jitterRNG;

        private const string _missingBoundsDefinerError = "Map Area Bounds Definer is not assigned.";
        private const string _missingBoundsComponentError = "Map Area Bounds Definer must have RectTransform or BoxCollider component.";
        private const string _missingNodeViewInterfaceError = "Node View Prefab does not have a component that implements IMapNodeView.";

        #region Public APIs

        public void Initialize(System.Random jitterRNG)
        {
            _jitterRNG = jitterRNG;
        }

        /// <summary>
        /// Creates a purely logical grid of MapNodes and calculates their world-space positions based on the defined bounds and direction.
        /// </summary>
        /// <returns>The created map node grid.</returns>
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

            return _mapGrid;
        }

        /// <summary>
        /// Instantiates the node view prefabs for each MapNode in the grid and initializes them with their corresponding MapNode data.
        /// </summary>
        public void CreateNodeViews()
        {
            if (_mapGrid == null) return;

            Quaternion rotation = Quaternion.Euler(
                _mapAreaBoundsDefiner.rotation.eulerAngles.x,
                _mapAreaBoundsDefiner.rotation.eulerAngles.y,
                _zRotation
            );

            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = _mapGrid[level, nodeIndex];
                    if (node == null) continue;

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
            }
        }

        /// <summary>
        /// Clears any nodes from the map grid that have no parent or child connections, effectively removing unused nodes from the grid.
        /// </summary>
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

        /// <summary>
        /// Destroys all instantiated node view GameObjects under the node view parent transform.
        /// </summary>
        public void ClearNodeViews()
        {
            foreach (Transform child in _nodeViewParent)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Calculates the bounds of the map area based on the defined bounds definer and direction.
        /// </summary>
        public void CalculateBounds()
        {
            if (_mapAreaBoundsDefiner == null)
            {
                Debug.LogError(_missingBoundsDefinerError);
                return;
            }

            MapBoundsData boundData = new MapBoundsData();

            // Determine the size and center of the bounds based on the component type
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

            // Calculate the right and up vectors based on the bounds definer's orientation and size
            boundData.right = _mapAreaBoundsDefiner.right * (boundData.size.x * 0.5f);
            boundData.up = _mapAreaBoundsDefiner.up * (boundData.size.y * 0.5f);

            // Adjust the origin based on the specified direction
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

            // Calculate dynamic spacing based on the number of nodes and levels
            // Normalized between 0 and 1 so that it can be scaled to the bounds size later in GetNodePosition
            _dynamicSpacing = new Vector2(
                _nodesPerLevel > 1 ? 1f / (_nodesPerLevel - 1) : 0.5f,
                _maxLevels > 1 ? 1f / (_maxLevels - 1) : 0.5f
            );
        }

        #endregion Public APIs

        #region Helpers

        /// <summary>
        /// Calculates the world-space position of a node based on its level and index, applying jitter if specified.
        /// </summary>
        /// <param name="level">The level of the node (row index).</param>
        /// <param name="nodeIndex">The index of the node within the level (column index).</param>
        /// <param name="applyJitter">Whether to apply jitter to the node's position.</param>
        /// <returns>The world-space position of the node.</returns>
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
                // Multiplier conversion for percentages
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

            // Draw index labels
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