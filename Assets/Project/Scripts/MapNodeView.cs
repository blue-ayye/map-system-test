using TMPro;
using UnityEngine;

namespace BP.MapSystem
{
    public class MapNodeView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _iconImage;
        [SerializeField] private TMP_Text _displayText; // Will be used for debug info but can be repurposed

        public void SetNodeType(MapNodeTypeSO nodeType)
        {
            _iconImage.sprite = nodeType.DisplayIcon;
            _displayText.text = nodeType.DisplayName;
        }
    }
}