using PrimeTween;
using UnityEngine;

namespace BP.MapSystem
{
    public class WorldMapPathView : MonoBehaviour, IMapPathView
    {
        [Header("Path References")]
        [SerializeField] private LineRenderer _baseLineRenderer;
        [SerializeField] private LineRenderer _traversedLineRenderer;

        private Vector3 _startLocalPos;
        private Vector3 _endLocalPos;

        public MapNode FromNode { get; private set; }
        public MapNode ToNode { get; private set; }

        #region Public APIs

        public void SetupPath(MapNode fromNode, MapNode toNode)
        {
            if (fromNode?.NodeView == null || toNode?.NodeView == null) return;

            FromNode = fromNode;
            ToNode = toNode;

            _startLocalPos = transform.InverseTransformPoint(fromNode.Position);
            _endLocalPos = transform.InverseTransformPoint(toNode.Position);

            _baseLineRenderer.positionCount = 2;
            _traversedLineRenderer.positionCount = 2;

            // Start BOTH positions at the origin so the lines are invisible initially
            _baseLineRenderer.SetPosition(0, _startLocalPos);
            _baseLineRenderer.SetPosition(1, _startLocalPos);

            _traversedLineRenderer.SetPosition(0, _startLocalPos);
            _traversedLineRenderer.SetPosition(1, _startLocalPos);
        }

        public Tween AnimateInitialDraw(float duration)
        {
            Tween.StopAll(_baseLineRenderer);

            return Tween.Custom(
                target: _baseLineRenderer,
                startValue: _startLocalPos,
                endValue: _endLocalPos,
                duration: duration,
                onValueChange: (lr, currentPos) => lr.SetPosition(1, currentPos),
                ease: Ease.InOutSine
            );
        }

        public Tween AnimateTraversal(float duration)
        {
            Tween.StopAll(_traversedLineRenderer);

            return Tween.Custom(
                target: _traversedLineRenderer,
                startValue: _startLocalPos,
                endValue: _endLocalPos,
                duration: duration,
                onValueChange: (lr, currentPos) => lr.SetPosition(1, currentPos),
                ease: Ease.InOutSine
            );
        }

        public void SetInstantlyTraversed()
        {
            Tween.StopAll(_traversedLineRenderer);
            _traversedLineRenderer.SetPosition(1, _endLocalPos);
        }

        public void ResetToDefault()
        {
            Tween.StopAll(_traversedLineRenderer);
            _traversedLineRenderer.SetPosition(1, _startLocalPos);
        }

        #endregion Public APIs
    }
}