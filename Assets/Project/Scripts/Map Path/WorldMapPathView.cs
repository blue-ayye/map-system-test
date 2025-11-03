using UnityEngine;

namespace BP.MapSystem
{
    [RequireComponent(typeof(LineRenderer))]
    public class WorldMapPathView : MonoBehaviour, IMapPathView
    {
        public void DrawPath(MapNode fromNode, MapNode toNode, Color? pathColor = null)
        {
            if (fromNode == null || toNode == null) return;
            if (fromNode.NodeView == null || toNode.NodeView == null) return;

            var startPos = fromNode.NodeView.Position;
            var endPos = toNode.NodeView.Position;

            var startLocalPos = transform.InverseTransformPoint(startPos);
            var endLocalPos = transform.InverseTransformPoint(endPos);

            var lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startLocalPos);
            lineRenderer.SetPosition(1, endLocalPos);

            if (pathColor.HasValue)
            {
                lineRenderer.startColor = pathColor.Value;
                lineRenderer.endColor = pathColor.Value;
            }
        }

        public void ChangePathColor(Color newColor)
        {
            var lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.startColor = newColor;
            lineRenderer.endColor = newColor;
        }
    }
}