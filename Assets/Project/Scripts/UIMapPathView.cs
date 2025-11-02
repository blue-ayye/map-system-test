using UnityEngine;
using UnityEngine.UI;

namespace BP.MapSystem
{
    [RequireComponent(typeof(Image))]
    public class UIMapPathView : MonoBehaviour, IMapPathView
    {
        public void DrawPath(MapNode fromNode, MapNode toNode, Color? pathColor = null)
        {
            if (fromNode == null || toNode == null) return;
            if (fromNode.NodeView == null || toNode.NodeView == null) return;

            var startPos = fromNode.NodeView.Position;
            var endPos = toNode.NodeView.Position;

            var startLocalPos = transform.InverseTransformPoint(startPos);
            var endLocalPos = transform.InverseTransformPoint(endPos);

            Vector2 direction = endLocalPos - startLocalPos; // Node: Since this is UI, we don't consider Z axis
            var distance = direction.magnitude;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            var pathImage = GetComponent<Image>();
            var rt = pathImage.rectTransform;
            rt.pivot = new Vector2(0, 0.5f);
            rt.localPosition = startLocalPos;
            rt.sizeDelta = new Vector2(distance, rt.sizeDelta.y);
            rt.rotation = Quaternion.Euler(0, 0, angle);

            if (pathColor.HasValue)
            {
                pathImage.color = pathColor.Value;
            }
            pathImage.raycastTarget = false;
        }

        public void ChangePathColor(Color newColor)
        {
            var pathImage = GetComponent<Image>();
            pathImage.color = newColor;
        }
    }
}