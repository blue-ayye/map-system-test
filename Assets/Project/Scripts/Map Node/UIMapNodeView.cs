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
        [SerializeField] private Image _iconImage;
        [SerializeField] private Transform _visitedStateIndicator;
        [SerializeField] private Transform _selectedStateIndicator;
        [SerializeField] private Color _lockedColor = Color.gray;
        [SerializeField] private Color _reachableColor = Color.white;
        [SerializeField] private TweenSettings<Vector3> _hoverEnterScaleTweenSettings = new TweenSettings<Vector3>(Vector3.one * 1.2f, 0.2f, Ease.OutBack);
        [SerializeField] private TweenSettings<Vector3> _hoverExitScaleTweenSettings = new TweenSettings<Vector3>(Vector3.one, 0.2f, Ease.OutBack);

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
            _hoverTween = Tween.Scale(transform, _hoverEnterScaleTweenSettings);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hoverTween.Stop();
            _hoverTween = Tween.Scale(transform, _hoverExitScaleTweenSettings);
        }

        #endregion Unity API

        #region Public APIs

        public void Initialize(MapNode node)
        {
            _mapNode = node;

            // Start visually hidden for the spawn animation
            transform.localScale = Vector3.zero;

            if (_iconImage != null)
                _iconImage.sprite = node.NodeType.DisplayIcon;
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
            _spawnTween = Tween.Scale(transform, Vector3.one, duration: nodeSpawnDuration, ease: Ease.OutBack);
            return _spawnTween;
        }

        #endregion Public APIs

        #region UI Updates

        private void UpdateUI(NodeState state)
        {
            if (_iconImage != null)
                _iconImage.color = state == NodeState.Locked ? _lockedColor : _reachableColor;

            if (_visitedStateIndicator != null)
                _visitedStateIndicator.gameObject.SetActive(state == NodeState.Visited);

            if (_selectedStateIndicator != null)
                _selectedStateIndicator.gameObject.SetActive(state == NodeState.Current);
        }

        #endregion UI Updates
    }
}