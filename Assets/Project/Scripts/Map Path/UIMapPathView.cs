using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace BP.MapSystem
{
    [RequireComponent(typeof(Image), typeof(RectTransform))]
    public class UIMapPathView : MonoBehaviour, IMapPathView
    {
        [SerializeField] private Color _defaultPathColor = Color.white;
        [SerializeField] private Color _traversedPathColor = Color.green;

        private float _targetDistance;
        private Image _pathImage;
        private RectTransform _rectTransform;

        public MapNode FromNode { get; private set; }
        public MapNode ToNode { get; private set; }

        #region Unity API

        private void Awake()
        {
            _pathImage = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
        }

        #endregion Unity API

        #region Public APIs

        public void DrawPath(MapNode fromNode, MapNode toNode, Color? pathColor = null)
        {
            if (fromNode == null || toNode == null) return;
            if (fromNode.NodeView == null || toNode.NodeView == null) return;

            FromNode = fromNode;
            ToNode = toNode;

            var startLocalPos = transform.InverseTransformPoint(fromNode.Position);
            var endLocalPos = transform.InverseTransformPoint(toNode.Position);

            Vector2 direction = endLocalPos - startLocalPos;
            _targetDistance = direction.magnitude;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            _rectTransform.pivot = new Vector2(0, 0.5f);
            _rectTransform.localPosition = startLocalPos;
            _rectTransform.sizeDelta = new Vector2(0, _rectTransform.sizeDelta.y);
            _rectTransform.rotation = Quaternion.Euler(0, 0, angle);

            if (pathColor.HasValue)
            {
                _pathImage.color = pathColor.Value;
            }

            _pathImage.raycastTarget = false;
        }

        public Tween AnimateDraw(float duration)
        {
            Tween.StopAll(_rectTransform);
            return Tween.UISizeDelta(_rectTransform, new Vector2(_targetDistance, _rectTransform.sizeDelta.y), duration, ease: Ease.InOutSine);
        }

        public void ChangePathColor(Color newColor)
        {
            _pathImage.color = newColor;
        }

        public void SetTraversedColor() => ChangePathColor(_traversedPathColor);

        public void SetDefaultColor() => ChangePathColor(_defaultPathColor);

        #endregion Public APIs
    }
}