using PrimeTween;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BP.MapSystem
{
    public class WorldMapNodeView : MonoBehaviour, IMapNodeView, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<MapNode> OnNodeClicked;

        public event Action<NodeState> OnStateChanged;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _iconRenderer;
        [SerializeField] private Transform _visitedStateIndicator;
        [SerializeField] private Transform _selectedStateIndicator;
        [SerializeField] private Color _lockedColor = Color.gray;
        [SerializeField] private Color _reachableColor = Color.white;

        [Header("Hover Animation")]
        [SerializeField] private float _hoverScaleFactor = 1.2f;
        [SerializeField] private TweenSettings<Vector3> _hoverTweenSettings;

        [Header("Spawn Animation")]
        [SerializeField] private TweenSettings<Vector3> _spawnTweenSettings;

        private MapNode _mapNode;
        private Tween _howerTween;
        private Tween _spawnTween;

        public Transform Transform => transform;

        #region Unity API

        public void OnPointerClick(PointerEventData eventData)
        {
            OnNodeClicked?.Invoke(_mapNode);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _howerTween.Stop();
            _hoverTweenSettings.startValue = transform.localScale;
            _hoverTweenSettings.endValue = _mapNode.Scale * _hoverScaleFactor;
            _howerTween = Tween.Scale(transform, _hoverTweenSettings);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _howerTween.Stop();
            _hoverTweenSettings.startValue = transform.localScale;
            _hoverTweenSettings.endValue = _mapNode.Scale;
            _howerTween = Tween.Scale(transform, _hoverTweenSettings);
        }

        #endregion Unity API

        #region Public APIs

        public void Initialize(MapNode node)
        {
            _mapNode = node;
            transform.localScale = Vector3.zero;

            if (_iconRenderer != null)
                _iconRenderer.sprite = node.NodeType.DisplayIcon;
        }

        public void SetState(NodeState state)
        {
            _mapNode.State = state;
            UpdateUI(state);
            OnStateChanged?.Invoke(state);
        }

        public Tween AnimateSpawn(float nodeSpawnDuration)
        {
            _spawnTween.Stop();
            _spawnTweenSettings.settings.duration = nodeSpawnDuration;
            _spawnTweenSettings.startValue = transform.localScale;
            _spawnTweenSettings.endValue = _mapNode.Scale;
            _spawnTween = Tween.Scale(transform, _spawnTweenSettings);
            return _spawnTween;
        }

        #endregion Public APIs

        #region Visual Updates

        private void UpdateUI(NodeState state)
        {
            if (_iconRenderer != null)
                _iconRenderer.color = state == NodeState.Locked ? _lockedColor : _reachableColor;

            if (_visitedStateIndicator != null)
                _visitedStateIndicator.gameObject.SetActive(state == NodeState.Visited);

            if (_selectedStateIndicator != null)
                _selectedStateIndicator.gameObject.SetActive(state == NodeState.Current);
        }

        #endregion Visual Updates
    }
}