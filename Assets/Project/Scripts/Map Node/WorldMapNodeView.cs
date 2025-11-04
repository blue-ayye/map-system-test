using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BP.MapSystem
{
    public class WorldMapNodeView : MonoBehaviour, IMapNodeView, IPointerClickHandler
    {
        public event Action<MapNode> OnNodeClicked;

        [SerializeField] private SpriteRenderer _iconRenderer;

        private MapNode _mapNode;

        public Transform Transform => transform;

        public void Initialize(MapNode node)
        {
            _mapNode = node;

            if (_iconRenderer != null)
                _iconRenderer.sprite = node.NodeType.DisplayIcon;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnNodeClicked?.Invoke(_mapNode);
        }
    }
}