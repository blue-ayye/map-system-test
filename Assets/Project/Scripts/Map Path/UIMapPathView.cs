using PrimeTween;
using UnityEngine;

namespace BP.MapSystem
{
    [RequireComponent(typeof(RectTransform))]
    public class UIMapPathView : MonoBehaviour, IMapPathView
    {
        [Header("Path Visuals")]
        [Tooltip("The RectTransform representing the underlying, unvisited path line.")]
        [SerializeField] private RectTransform _basePathRect;
        [Tooltip("The RectTransform representing the highlighted line that fills as the player travels.")]
        [SerializeField] private RectTransform _traversedPathRect;

        private RectTransform _rootRectTransform;
        private float _targetDistance;
        private Tween _initialDrawTween;
        private Tween _traversalTween;

        public MapNode FromNode { get; private set; }
        public MapNode ToNode { get; private set; }

        #region Unity API

        private void Awake()
        {
            _rootRectTransform = GetComponent<RectTransform>();
        }

        #endregion Unity API

        #region Initialization

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
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Position and rotate the root container to point towards the target
            _rootRectTransform.localPosition = startLocalPos;
            _rootRectTransform.localRotation = Quaternion.Euler(0, 0, angle);

            // Set both child pivots to the left edge so they scale outward from the start node
            _basePathRect.pivot = new Vector2(0, 0.5f);
            _traversedPathRect.pivot = new Vector2(0, 0.5f);

            // Reset both child widths to 0 for initial hidden state
            _basePathRect.sizeDelta = new Vector2(0, _basePathRect.sizeDelta.y);
            _traversedPathRect.sizeDelta = new Vector2(0, _traversedPathRect.sizeDelta.y);
        }

        #endregion Initialization

        #region State Management

        public void SetInstantlyTraversed()
        {
            _traversalTween.Stop();
            _traversedPathRect.sizeDelta = new Vector2(_targetDistance, _traversedPathRect.sizeDelta.y);
        }

        public void ResetToDefault()
        {
            _traversalTween.Stop();
            _traversedPathRect.sizeDelta = new Vector2(0, _traversedPathRect.sizeDelta.y);
        }

        #endregion State Management

        #region Animation

        public Tween AnimateInitialDraw(float duration)
        {
            _initialDrawTween.Stop();
            _initialDrawTween = Tween.UISizeDelta(_basePathRect, new Vector2(_targetDistance, _basePathRect.sizeDelta.y), duration, ease: Ease.InOutSine);
            return _initialDrawTween;
        }

        public Tween AnimateTraversal(float duration)
        {
            _traversalTween.Stop();
            _traversalTween = Tween.UISizeDelta(_traversedPathRect, new Vector2(_targetDistance, _traversedPathRect.sizeDelta.y), duration, ease: Ease.InOutSine);
            return _traversalTween;
        }

        #endregion Animation
    }
}