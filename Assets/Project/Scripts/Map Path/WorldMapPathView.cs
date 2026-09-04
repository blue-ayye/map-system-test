using PrimeTween;
using UnityEngine;

namespace BP.MapSystem
{
    public class WorldMapPathView : MonoBehaviour, IMapPathView
    {
        [Header("Path References")]
        [SerializeField] private LineRenderer _baseLineRenderer;
        [SerializeField] private LineRenderer _traversedLineRenderer;

        [Header("Path Animation Settings")]
        [SerializeField] private TweenSettings<Vector3> _initialDrawTweenSettings;
        [SerializeField] private TweenSettings<Vector3> _traversalTweenSettings;

        private Vector3 _startLocalPos;
        private Vector3 _endLocalPos;
        private Tween _initialDrawTween;
        private Tween _traversalTween;

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
            _initialDrawTween.Stop();

            _initialDrawTweenSettings.settings.duration = duration;
            _initialDrawTweenSettings.startValue = _startLocalPos;
            _initialDrawTweenSettings.endValue = _endLocalPos;

            _initialDrawTween = Tween.Custom(_baseLineRenderer, _initialDrawTweenSettings, (lr, currentPos) => lr.SetPosition(1, currentPos));
            return _initialDrawTween;
        }

        public Tween AnimateTraversal(float duration)
        {
            _traversalTween.Stop();

            _traversalTweenSettings.settings.duration = duration;
            _traversalTweenSettings.startValue = _startLocalPos;
            _traversalTweenSettings.endValue = _endLocalPos;

            _traversalTween = Tween.Custom(_traversedLineRenderer, _traversalTweenSettings, (lr, currentPos) => lr.SetPosition(1, currentPos));
            return _traversalTween;
        }

        public void SetInstantlyTraversed()
        {
            _traversalTween.Stop();
            _traversedLineRenderer.SetPosition(1, _endLocalPos);
        }

        public void ResetToDefault()
        {
            _traversalTween.Stop();
            _traversedLineRenderer.SetPosition(1, _startLocalPos);
        }

        #endregion Public APIs
    }
}