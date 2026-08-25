using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace BP.MapSystem
{
    [RequireComponent(typeof(Image))]
    public class UIMapPathView : MonoBehaviour, IMapPathView
    {
        [SerializeField] private Color _defaultPathColor = Color.white;
        [SerializeField] private Color _traversedPathColor = Color.green;

        public MapNode FromNode { get; private set; }
        public MapNode ToNode { get; private set; }

        private float _targetDistance; // Cache the distance for the tween

        public void DrawPath(MapNode fromNode, MapNode toNode, Color? pathColor = null)
        {
            if (fromNode == null || toNode == null) return;
            if (fromNode.NodeView == null || toNode.NodeView == null) return;

            FromNode = fromNode;
            ToNode = toNode;

            var startLocalPos = transform.InverseTransformPoint(fromNode.Position);
            var endLocalPos = transform.InverseTransformPoint(toNode.Position);

            Vector2 direction = endLocalPos - startLocalPos; // Node: Since this is UI, we don't consider Z axis
            var distance = direction.magnitude;
            _targetDistance = distance; // Cache the distance for the tween
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            var pathImage = GetComponent<Image>();
            var rt = pathImage.rectTransform;
            rt.pivot = new Vector2(0, 0.5f);
            rt.localPosition = startLocalPos;
            rt.sizeDelta = new Vector2(0, rt.sizeDelta.y); // Start with width 0 for the tween
            rt.rotation = Quaternion.Euler(0, 0, angle);

            if (pathColor.HasValue)
            {
                pathImage.color = pathColor.Value;
            }
            pathImage.raycastTarget = false;
        }

        public Tween AnimateDraw(float duration)
        {
            var rt = GetComponent<RectTransform>();
            Tween.StopAll(rt);

            return Tween.UISizeDelta(rt, new Vector2(_targetDistance, rt.sizeDelta.y), duration, ease: Ease.InOutSine);
        }

        public void ChangePathColor(Color newColor)
        {
            var pathImage = GetComponent<Image>();
            pathImage.color = newColor;
        }

        public void SetTraversedColor()
        {
            ChangePathColor(_traversedPathColor);
        }

        public void SetDefaultColor()
        {
            ChangePathColor(_defaultPathColor);
        }
    }
}