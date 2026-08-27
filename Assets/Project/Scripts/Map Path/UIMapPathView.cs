using PrimeTween;
using UnityEngine;

namespace BP.MapSystem
{
    [RequireComponent(typeof(RectTransform))]
    public class UIMapPathView : MonoBehaviour, IMapPathView
    {
        [Header("Path References")]
        [SerializeField] private RectTransform _basePathRect;
        [SerializeField] private RectTransform _traversedPathRect;

        private RectTransform _rootRectTransform;
        private float _targetDistance;

        public MapNode FromNode { get; private set; }
        public MapNode ToNode { get; private set; }

        #region Unity API

        private void Awake()
        {
            _rootRectTransform = GetComponent<RectTransform>();
        }

        #endregion Unity API

        #region Public APIs

        public void SetupPath(MapNode fromNode, MapNode toNode)
        {
            if (fromNode?.NodeView == null || toNode?.NodeView == null) return;

            FromNode = fromNode;
            ToNode = toNode;

            // Calculate positions relative to the parent container
            var startLocalPos = transform.parent.InverseTransformPoint(fromNode.Position);
            var endLocalPos = transform.parent.InverseTransformPoint(toNode.Position);

            Vector2 direction = endLocalPos - startLocalPos;
            _targetDistance = direction.magnitude;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Position and rotate the root container
            _rootRectTransform.localPosition = startLocalPos;
            _rootRectTransform.localRotation = Quaternion.Euler(0, 0, angle);

            // Set both child pivots to the left edge
            _basePathRect.pivot = new Vector2(0, 0.5f);
            _traversedPathRect.pivot = new Vector2(0, 0.5f);

            // Reset both child widths to 0
            _basePathRect.sizeDelta = new Vector2(0, _basePathRect.sizeDelta.y);
            _traversedPathRect.sizeDelta = new Vector2(0, _traversedPathRect.sizeDelta.y);
        }

        public Tween AnimateInitialDraw(float duration)
        {
            Tween.StopAll(_basePathRect);
            return Tween.UISizeDelta(_basePathRect, new Vector2(_targetDistance, _basePathRect.sizeDelta.y), duration, ease: Ease.InOutSine);
        }

        public Tween AnimateTraversal(float duration)
        {
            Tween.StopAll(_traversedPathRect);
            return Tween.UISizeDelta(_traversedPathRect, new Vector2(_targetDistance, _traversedPathRect.sizeDelta.y), duration, ease: Ease.InOutSine);
        }

        public void SetInstantlyTraversed()
        {
            Tween.StopAll(_traversedPathRect);
            _traversedPathRect.sizeDelta = new Vector2(_targetDistance, _traversedPathRect.sizeDelta.y);
        }

        public void ResetToDefault()
        {
            Tween.StopAll(_traversedPathRect);
            _traversedPathRect.sizeDelta = new Vector2(0, _traversedPathRect.sizeDelta.y);
        }

        #endregion Public APIs
    }
}