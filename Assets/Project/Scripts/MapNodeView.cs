using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BP.MapSystem
{
    public class MapNodeView : MonoBehaviour
    {
        [SerializeField] private GameObject _iconImage;
        [SerializeField] private TMP_Text _displayText; // Will be used for debug info but can be repurposed

        public void SetNodeType(MapNodeTypeSO nodeType)
        {
            if (_iconImage.TryGetComponent(out Image image))
            {
                image.sprite = nodeType.DisplayIcon;
            }
            else if (_iconImage.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                spriteRenderer.sprite = nodeType.DisplayIcon;
            }

            if (_displayText != null)
                _displayText.text = nodeType.DisplayName;
        }
    }
}