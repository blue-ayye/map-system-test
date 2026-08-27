using PrimeTween;
using UnityEngine;

namespace BP.MapSystem
{
    [RequireComponent(typeof(LineRenderer))]
    public class WorldMapPathView : MonoBehaviour, IMapPathView
    {
        [SerializeField] private Color _defaultPathColor = Color.white;
        [SerializeField] private Color _traversedPathColor = Color.green;

        private Vector3 _startLocalPos;
        private Vector3 _endLocalPos;
        private LineRenderer _lineRenderer;

        public MapNode FromNode { get; private set; }
        public MapNode ToNode { get; private set; }

        #region Unity API

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        #endregion Unity API

        #region Public APIs

        public void DrawPath(MapNode fromNode, MapNode toNode, Color? pathColor = null)
        {
            if (fromNode == null || toNode == null) return;
            if (fromNode.NodeView == null || toNode.NodeView == null) return;

            FromNode = fromNode;
            ToNode = toNode;

            _startLocalPos = transform.InverseTransformPoint(fromNode.Position);
            _endLocalPos = transform.InverseTransformPoint(toNode.Position);

            _lineRenderer.positionCount = 2;

            // Start BOTH positions at the origin so the line is effectively invisible initially
            _lineRenderer.SetPosition(0, _startLocalPos);
            _lineRenderer.SetPosition(1, _startLocalPos);

            if (pathColor.HasValue)
            {
                _lineRenderer.startColor = pathColor.Value;
                _lineRenderer.endColor = pathColor.Value;
            }
        }

        public Tween AnimateDraw(float duration)
        {
            Tween.StopAll(_lineRenderer);

            return Tween.Custom(
                target: _lineRenderer,
                startValue: _startLocalPos,
                endValue: _endLocalPos,
                duration: duration,
                onValueChange: (lr, currentPos) => lr.SetPosition(1, currentPos),
                ease: Ease.InOutSine
            );
        }

        public void ChangePathColor(Color newColor)
        {
            _lineRenderer.startColor = newColor;
            _lineRenderer.endColor = newColor;
        }

        public void SetTraversedColor() => ChangePathColor(_traversedPathColor);

        public void SetDefaultColor() => ChangePathColor(_defaultPathColor);

        #endregion Public APIs
    }
}