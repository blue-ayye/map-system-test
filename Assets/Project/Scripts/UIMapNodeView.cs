using UnityEngine;
using UnityEngine.UI;

namespace BP.MapSystem
{
    public class UIMapNodeView : MonoBehaviour, IMapNodeView
    {
        [SerializeField] private Image _iconImage;

        public Vector3 Position => transform.position;

        public void SetNodeType(MapNodeTypeSO nodeType)
        {
            if (_iconImage != null)
                _iconImage.sprite = nodeType.DisplayIcon;
        }
    }
}