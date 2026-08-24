using PrimeTween;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BP.MapSystem
{
    public class UIMapNodeView : MonoBehaviour, IMapNodeView, IPointerClickHandler
    {
        public event Action<MapNode> OnNodeClicked;

        [SerializeField] private Image _iconImage;
        [SerializeField] private Transform _visitedStateIndicator;
        [SerializeField] private Transform _selectedStateIndicator;

        private MapNode _mapNode;

        public Transform Transform => transform;

        public void Initialize(MapNode node)
        {
            _mapNode = node;

            // Start visually hidden for the spawn animation
            transform.localScale = Vector3.zero;

            if (_iconImage != null)
                _iconImage.sprite = node.NodeType.DisplayIcon;
            if (_visitedStateIndicator != null)
                _visitedStateIndicator.gameObject.SetActive(false);
            if (_selectedStateIndicator != null)
                _selectedStateIndicator.gameObject.SetActive(false);
        }

        public void SetActiveVisitedState(bool state)
        {
            if (_visitedStateIndicator != null)
                _visitedStateIndicator.gameObject.SetActive(state);
        }

        public void SetActiveSelectedState(bool state)
        {
            if (_selectedStateIndicator != null)
                _selectedStateIndicator.gameObject.SetActive(state);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnNodeClicked?.Invoke(_mapNode);
        }

        public void AnimateSpawn(float delay, float duration)
        {
            if (delay <= 0f || duration <= 0f)
            {
                transform.localScale = Vector3.one;
                return;
            }

            Tween.StopAll(transform);

            Tween.Scale(transform, Vector3.one, duration: duration, ease: Ease.OutBack, startDelay: delay);
        }
    }
}