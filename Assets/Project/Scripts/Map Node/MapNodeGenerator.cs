using System;
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

    public struct MapBoundsData
    {
        public Vector3 origin;
        public Vector3 right;
        public Vector3 up;
        public Vector3 center;
        public Vector3 size;
    }

    public class MapNodeGenerator : MonoBehaviour
    {
        #region Fields

        [Header("Map Grid Settings")]
        [SerializeField] private int _maxLevels = 9;
        [SerializeField] private int _nodesPerLevel = 7;
        [SerializeField] private Transform _mapAreaBoundsDefiner;
        [SerializeField] private MapDirection _direction = MapDirection.TopToBottom;
        [Tooltip("0.25f means up to 25% of the distance between nodes (they will never overlap)")]
        [SerializeField][Range(0f, .5f)] private float _nodeSpaceJitterAmount;
        [Tooltip("0.25f means up to 25% of the distance between levels (they will never overlap)")]
        [SerializeField][Range(0f, .5f)] private float _levelSpaceJitterAmount;
        [SerializeField] private bool _applyJitter = true;

        [Header("Node View Settings")]
        [SerializeField] private Transform _nodeViewParent;
        [SerializeField] private Transform _nodeViewPrefab;
        [SerializeField] private int _zRotation;

        private MapNode[,] _mapGrid;
        private MapBoundsData _bounds;
        private Vector2 _dynamicSpacing;
        private System.Random _jitterRNG;

        #endregion

        #region Unity Lifecycle

        private void OnDrawGizmos()
        {
            if (_mapAreaBoundsDefiner == null) return;

            // Use a local variable so editor repaints never corrupt the runtime _bounds/_dynamicSpacing fields.
            MapBoundsData gizmoBounds = ComputeBounds();
            Vector2 gizmoSpacing = new Vector2(
                _nodesPerLevel > 1 ? 1f / (_nodesPerLevel - 1) : 0.5f,
                _maxLevels > 1 ? 1f / (_maxLevels - 1) : 0.5f
            );

            float areaDiagonal = gizmoBounds.size.magnitude;
            float radius = areaDiagonal * 0.01f;

            // Base gizmos
            Gizmos.color = Color.blue; // Start point
            Gizmos.DrawLine(gizmoBounds.origin, gizmoBounds.origin + gizmoBounds.right * 2f);
            Gizmos.color = Color.red; // End point
            Gizmos.DrawLine(gizmoBounds.origin + gizmoBounds.up * 2f, gizmoBounds.origin + gizmoBounds.right * 2f + gizmoBounds.up * 2f);
            Gizmos.color = Color.green; // Sides
            Gizmos.DrawLine(gizmoBounds.origin, gizmoBounds.origin + gizmoBounds.up * 2f);
            Gizmos.DrawLine(gizmoBounds.origin + gizmoBounds.right * 2f, gizmoBounds.origin + gizmoBounds.right * 2f + gizmoBounds.up * 2f);

            // Draw node positions
            Gizmos.color = Color.yellow;
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    Vector3 pos = GetNodePositionFromBounds(gizmoBounds, gizmoSpacing, level, nodeIndex);
                    Gizmos.DrawSphere(pos, radius);
                }
            }

            // Draw index labels
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            for (int i = 0; i < _nodesPerLevel; i++)
            {
                Vector3 pos = GetNodePositionFromBounds(gizmoBounds, gizmoSpacing, -1, i);
                UnityEditor.Handles.Label(pos + Vector3.up * radius * 1.5f, $"N{i}");
            }

            for (int i = 0; i < _maxLevels; i++)
            {
                Vector3 pos = GetNodePositionFromBounds(gizmoBounds, gizmoSpacing, i, -1);
                UnityEditor.Handles.Label(pos + Vector3.up * radius * 1.5f, $"L{i}");
            }
#endif
        }

        #endregion

        #region Public API

        /// <summary>
        /// Writes the grid parameters required to reproduce identical node positions
        /// into <paramref name="mapData"/>. Node type data is written by
        /// <see cref="MapNodeTypeAssigner.WriteTo"/>.
        /// </summary>
        public void WriteTo(MapData mapData)
        {
            // Persist grid parameters so the map can be reproduced identically on load
            // even if inspector defaults change between versions.
            mapData.GridParameters = new GridParameters
            {
                MaxLevels = _maxLevels,
                NodesPerLevel = _nodesPerLevel,
                NodeSpaceJitterAmount = _nodeSpaceJitterAmount,
                LevelSpaceJitterAmount = _levelSpaceJitterAmount
            };
        }

        /// <summary>
        /// Restores grid parameters from <paramref name="mapData"/> so that the next
        /// <see cref="CreateNodeGrid"/> call produces positions matching those at save time.
        /// </summary>
        public void ReadFrom(MapData mapData)
        {
            var gp = mapData.GridParameters;
            _maxLevels = gp.MaxLevels;
            _nodesPerLevel = gp.NodesPerLevel;
            _nodeSpaceJitterAmount = gp.NodeSpaceJitterAmount;
            _levelSpaceJitterAmount = gp.LevelSpaceJitterAmount;
        }

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
                    MapNode node = new MapNode(level, nodeIndex);
                    node.Position = GetNodePosition(level, nodeIndex, _applyJitter);

                    _mapGrid[level, nodeIndex] = node;
                }
            }

            return _mapGrid;
        }

        /// <summary>
        /// Nulls out every grid slot whose node has no parent or child connections,
        /// removing nodes that were not included in any generated path.
        /// <para>
        /// <b>Shared reference contract:</b> <c>_mapGrid</c> is the same array instance
        /// passed to <see cref="MapPathGenerator"/>, <see cref="MapNodeTypeAssigner"/>, and
        /// <see cref="MapTraversalController"/> via their <c>Initialize</c> methods. Setting a
        /// slot to <c>null</c> here is immediately visible to all those holders.
        /// </para>
        /// </summary>
        public void ClearUnusedNodes()
        {
            for (int level = 0; level < _maxLevels; level++)
            {
                for (int nodeIndex = 0; nodeIndex < _nodesPerLevel; nodeIndex++)
                {
                    var node = _mapGrid[level, nodeIndex];
                    if (node != null && node.ParentNodes.Count == 0 && node.ChildNodes.Count == 0)
                        _mapGrid[level, nodeIndex] = null;
                }
            }
        }

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

                    var nodeViewTransform = Instantiate(_nodeViewPrefab, _nodeViewParent);
                    nodeViewTransform.SetPositionAndRotation(node.Position, rotation);

                    if (nodeViewTransform.TryGetComponent(out IMapNodeView nodeView))
                    {
                        nodeView.Initialize(node);
                        node.NodeView = nodeView;
                    }
                    else
                    {
                        Debug.LogError("Node View Prefab does not have a component that implements IMapNodeView.");
                    }
                }
            }
        }

        public void ClearNodeViews()
        {
            foreach (Transform child in _nodeViewParent)
                Destroy(child.gameObject);
        }

        /// <summary>
        /// Computes and caches bounds and dynamic spacing from the bounds-definer transform.
        /// Must be called before <see cref="CreateNodeGrid"/> or any position query.
        /// </summary>
        public void CalculateBounds()
        {
            _bounds = ComputeBounds();
            _dynamicSpacing = new Vector2(
                _nodesPerLevel > 1 ? 1f / (_nodesPerLevel - 1) : 0.5f,
                _maxLevels > 1 ? 1f / (_maxLevels - 1) : 0.5f
            );
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Computes bounds from the bounds-definer transform and returns the result as a local
        /// value. Does not write to any instance fields, making it safe to call from editor-only
        /// contexts such as <c>OnDrawGizmos</c>.
        /// </summary>
        private MapBoundsData ComputeBounds()
        {
            if (_mapAreaBoundsDefiner == null)
            {
                Debug.LogError("Map Area Bounds Definer is not assigned.");
                return default;
            }

            MapBoundsData boundData = new MapBoundsData();

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
                return default;
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
                    (boundData.right, boundData.up) = (boundData.up, boundData.right); // Rotate 90 degrees clockwise
                    boundData.origin = boundData.center - boundData.right - boundData.up;
                    break;

                case MapDirection.RightToLeft:
                    (boundData.right, boundData.up) = (-boundData.up, -boundData.right); // Rotate 90 degrees counter-clockwise
                    boundData.origin = boundData.center - boundData.right - boundData.up;
                    break;
            }

            return boundData;
        }

        private Vector3 GetNodePosition(int level, int nodeIndex, bool applyJitter = false)
        {
            return GetNodePositionFromBounds(_bounds, _dynamicSpacing, level, nodeIndex, applyJitter);
        }

        private Vector3 GetNodePositionFromBounds(MapBoundsData bounds, Vector2 spacing, int level, int nodeIndex, bool applyJitter = false)
        {
            float xNorm = spacing.x * nodeIndex;
            float yNorm = spacing.y * level;

            if (!applyJitter)
            {
                return bounds.origin
                    + bounds.right * xNorm * 2f
                    + bounds.up * yNorm * 2f;
            }
            else
            {
                float jitterX = (float)(_jitterRNG.NextDouble() * 2f - 1f) * spacing.x * _nodeSpaceJitterAmount;
                float jitterY = (float)(_jitterRNG.NextDouble() * 2f - 1f) * spacing.y * _levelSpaceJitterAmount;

                return bounds.origin
                    + bounds.right * (xNorm + jitterX) * 2f
                    + bounds.up * (yNorm + jitterY) * 2f;
            }
        }

        #endregion

        #region Editor / Debug

        [ContextMenu("Debug Create Map Grid")]
        private void DebugCreateMapGrid()
        {
            ClearNodeViews();
            CreateNodeGrid();
            CreateNodeViews();
        }

        #endregion
    }
}
