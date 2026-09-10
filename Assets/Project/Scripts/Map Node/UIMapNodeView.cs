using PrimeTween;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BP.MapSystem
{
    public class UIMapNodeView : MonoBehaviour, IMapNodeView, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<MapNode> OnNodeClicked;

        public event Action<NodeState> OnStateChanged;

        [Header("Visuals")]
        [Tooltip("The main image component reflecting the node type icon.")]
        [SerializeField] private Image _iconImage;
        [Tooltip("Object enabled when this node has already been traveled to.")]
        [SerializeField] private Transform _visitedStateIndicator;
        [Tooltip("Object enabled when the player is currently sitting on this node.")]
        [SerializeField] private Transform _selectedStateIndicator;
        [Tooltip("Color applied when the node is inaccessible from the current location.")]
        [SerializeField] private Color _lockedColor = Color.gray;
        [Tooltip("Color applied when the node is a valid next move.")]
        [SerializeField] private Color _reachableColor = Color.white;

        [Header("Hover Animation")]
        [Tooltip("Scale multiplier applied dynamically when hovering with the mouse.")]
        [SerializeField] private float _hoverScaleFactor = 1.2f;
        [Tooltip("Tween configuration for the hover scale transition.")]
        [SerializeField] private TweenSettings<Vector3> _hoverTweenSettings;

        [Header("Spawn Animation")]
        [Tooltip("Tween configuration for the node's initial reveal on map load.")]
        [SerializeField] private TweenSettings<Vector3> _spawnTweenSettings;

        private MapNode _mapNode;
        private Tween _hoverTween;
        private Tween _spawnTween;

        public Transform Transform => transform;

        #region Unity API

        public void OnPointerClick(PointerEventData eventData)
        {
            OnNodeClicked?.Invoke(_mapNode);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hoverTween.Stop();
            _hoverTweenSettings.startValue = transform.localScale;
            _hoverTweenSettings.endValue = _mapNode.Scale * _hoverScaleFactor;
            _hoverTween = Tween.Scale(transform, _hoverTweenSettings);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hoverTween.Stop();
            _hoverTweenSettings.startValue = transform.localScale;
            _hoverTweenSettings.endValue = _mapNode.Scale;
            _hoverTween = Tween.Scale(transform, _hoverTweenSettings);
        }

        #endregion Unity API

        #region Initialization

        public void Initialize(MapNode node)
        {
            _mapNode = node;

            // Start visually hidden for the spawn animation
            transform.localScale = Vector3.zero;

            if (_iconImage != null)
            {
                _iconImage.sprite = node.NodeType.DisplayIcon;
            }
        }

        #endregion Initialization

        #region State Management

        public void SetState(NodeState state)
        {
            _mapNode.State = state;

            if (_iconImage != null)
            {
                _iconImage.color = state == NodeState.Locked ? _lockedColor : _reachableColor;
            }

            if (_visitedStateIndicator != null)
            {
                _visitedStateIndicator.gameObject.SetActive(state == NodeState.Visited);
            }

            if (_selectedStateIndicator != null)
            {
                _selectedStateIndicator.gameObject.SetActive(state == NodeState.Current);
            }

            OnStateChanged?.Invoke(state);
        }

        #endregion State Management

        #region Animation

        public Tween AnimateSpawn(float nodeSpawnDuration)
        {
            _spawnTween.Stop();
            _spawnTweenSettings.settings.duration = nodeSpawnDuration;
            _spawnTweenSettings.startValue = transform.localScale;
            _spawnTweenSettings.endValue = _mapNode.Scale;
            _spawnTween = Tween.Scale(transform, _spawnTweenSettings);
            return _spawnTween;
        }

        #endregion Animation
    }
}