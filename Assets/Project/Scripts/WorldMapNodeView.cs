using UnityEngine;

namespace BP.MapSystem
{
    public class WorldMapNodeView : MonoBehaviour, IMapNodeView
    {
        [SerializeField] private SpriteRenderer _iconRenderer;
        public Vector3 Position => transform.position;
        public void SetNodeType(MapNodeTypeSO nodeType)
        {
            if (_iconRenderer != null)
                _iconRenderer.sprite = nodeType.DisplayIcon;
        }
    }
}