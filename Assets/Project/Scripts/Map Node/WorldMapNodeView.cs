using UnityEngine;

namespace BP.MapSystem
{
    public class WorldMapNodeView : MonoBehaviour, IMapNodeView
    {
        [SerializeField] private SpriteRenderer _iconRenderer;

        private MapNode _mapNode;

        public Transform Transform => transform;

        public void Initialize(MapNode node)
        {
            _mapNode = node;

            if (_iconRenderer != null)
                _iconRenderer.sprite = node.NodeType.DisplayIcon;
        }
    }
}