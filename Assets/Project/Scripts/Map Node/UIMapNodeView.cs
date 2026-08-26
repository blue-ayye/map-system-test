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

        public Transform Transform => transform;

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

        private void UpdateUI(NodeState state)
        {
            if (_iconImage != null)
                _iconImage.color = state == NodeState.Locked ? _lockedColor : _reachableColor;

            if (_visitedStateIndicator != null)
                _visitedStateIndicator.gameObject.SetActive(state == NodeState.Visited);
            if (_selectedStateIndicator != null)
                _selectedStateIndicator.gameObject.SetActive(state == NodeState.Current);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnNodeClicked?.Invoke(_mapNode);
        }

        public Tween AnimateSpawn(float nodeSpawnDuration)
        {
            Tween.StopAll(transform);
            return Tween.Scale(transform, Vector3.one, duration: nodeSpawnDuration, ease: Ease.OutBack);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Tween.StopAll(transform);
            Tween.Scale(transform, _hoverEnterScaleTweenSettings);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Tween.StopAll(transform);
            Tween.Scale(transform, _hoverExitScaleTweenSettings);
        }
    }
}