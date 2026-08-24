using PrimeTween;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BP.MapSystem
{
    public class WorldMapNodeView : MonoBehaviour, IMapNodeView, IPointerClickHandler
    {
        public event Action<MapNode> OnNodeClicked;

        [SerializeField] private SpriteRenderer _iconRenderer;
        [SerializeField] private Transform _visitedStateIndicator;
        [SerializeField] private Transform _selectedStateIndicator;

        private MapNode _mapNode;

        public Transform Transform => transform;

        public void Initialize(MapNode node)
        {
            _mapNode = node;

            // Start visually hidden for the spawn animation
            transform.localScale = Vector3.zero;

            if (_iconRenderer != null)
                _iconRenderer.sprite = node.NodeType.DisplayIcon;
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

        public Tween AnimateSpawn(float nodeSpawnDuration)
        {
            Tween.StopAll(transform);
            return Tween.Scale(transform, Vector3.one, duration: nodeSpawnDuration, ease: Ease.OutBack);
        }
    }
}