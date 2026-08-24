using PrimeTween;
using UnityEngine;

namespace BP.MapSystem
{
    [RequireComponent(typeof(LineRenderer))]
    public class WorldMapPathView : MonoBehaviour, IMapPathView
    {
        public MapNode FromNode { get; private set; }
        public MapNode ToNode { get; private set; }

        private Vector3 _startLocalPos;
        private Vector3 _endLocalPos;

        public void DrawPath(MapNode fromNode, MapNode toNode, Color? pathColor = null)
        {
            if (fromNode == null || toNode == null) return;
            if (fromNode.NodeView == null || toNode.NodeView == null) return;

            FromNode = fromNode;
            ToNode = toNode;

            _startLocalPos = transform.InverseTransformPoint(fromNode.Position);
            _endLocalPos = transform.InverseTransformPoint(toNode.Position);

            var lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;

            // Start BOTH positions at the origin so the line is effectively invisible
            lineRenderer.SetPosition(0, _startLocalPos);
            lineRenderer.SetPosition(1, _startLocalPos);

            if (pathColor.HasValue)
            {
                lineRenderer.startColor = pathColor.Value;
                lineRenderer.endColor = pathColor.Value;
            }
        }

        public void AnimateDraw(float duration, float delay)
        {
            var lineRenderer = GetComponent<LineRenderer>();

            if (duration <= 0f)
            {
                lineRenderer.SetPosition(1, _endLocalPos);
                return;
            }

            Tween.StopAll(lineRenderer); // Safety cleanup

            Tween.Custom(
                target: lineRenderer,
                startValue: _startLocalPos,
                endValue: _endLocalPos,
                duration: duration,
                onValueChange: (lr, currentPos) => lr.SetPosition(1, currentPos),
                ease: Ease.InOutSine,
                startDelay: delay
            );
        }

        public void ChangePathColor(Color newColor)
        {
            var lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.startColor = newColor;
            lineRenderer.endColor = newColor;
        }
    }
}