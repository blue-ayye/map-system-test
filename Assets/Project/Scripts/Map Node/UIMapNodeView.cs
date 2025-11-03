using UnityEngine;
using UnityEngine.UI;

namespace BP.MapSystem
{
    public class UIMapNodeView : MonoBehaviour, IMapNodeView
    {
        [SerializeField] private Image _iconImage;

        private MapNode _mapNode;

        public Transform Transform => transform;

        public void Initialize(MapNode node)
        {
            _mapNode = node;

            if (_iconImage != null)
                _iconImage.sprite = node.NodeType.DisplayIcon;
        }
    }
}