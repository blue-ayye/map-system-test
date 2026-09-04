using UnityEngine;
using UnityEngine.UI;

namespace BP.MapSystem
{
    public class MapSystemDemo : MonoBehaviour
    {
        [SerializeField] private GameObject _uiMapPanel;
        [SerializeField] private GameObject _3DMapPanel;
        [SerializeField] private Toggle _uiMapToggle;
        [SerializeField] private Toggle _3DMapToggle;

        private void Awake()
        {
            _uiMapPanel.SetActive(false);
            _3DMapPanel.SetActive(false);

            _uiMapToggle.onValueChanged.RemoveAllListeners();
            _3DMapToggle.onValueChanged.RemoveAllListeners();
            _uiMapToggle.onValueChanged.AddListener(OnUIToggleChanged);
            _3DMapToggle.onValueChanged.AddListener(On3DToggleChanged);
        }

        private void Start()
        {
            _uiMapToggle.isOn = true;
            OnUIToggleChanged(_uiMapToggle.isOn);
        }

        private void OnUIToggleChanged(bool isOn)
        {
            if (isOn)
            {
                _uiMapPanel.SetActive(true);
                _3DMapPanel.SetActive(false);
            }
        }

        private void On3DToggleChanged(bool isOn)
        {
            if (isOn)
            {
                _uiMapPanel.SetActive(false);
                _3DMapPanel.SetActive(true);
            }
        }
    }
}