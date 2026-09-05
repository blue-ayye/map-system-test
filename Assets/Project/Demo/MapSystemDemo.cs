using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BP.MapSystem
{
    public class MapSystemDemo : MonoBehaviour
    {
        [SerializeField] private MapSystemManager _uiMapSystemManager;
        [SerializeField] private MapSystemManager _3DMapSystemManager;

        [SerializeField] private GameObject _uiMapPanel;
        [SerializeField] private GameObject _3DMapPanel;
        [SerializeField] private Toggle _uiMapToggle;
        [SerializeField] private Toggle _3DMapToggle;
        [SerializeField] private Button _generateMapButton;
        [SerializeField] private Toggle _randomSeedToggle;
        [SerializeField] private TMP_InputField _seedInputField;

        private void Awake()
        {
            _uiMapPanel.SetActive(false);
            _3DMapPanel.SetActive(false);

            _uiMapToggle.onValueChanged.RemoveAllListeners();
            _3DMapToggle.onValueChanged.RemoveAllListeners();
            _uiMapToggle.onValueChanged.AddListener(OnUIToggleChanged);
            _3DMapToggle.onValueChanged.AddListener(On3DToggleChanged);

            _generateMapButton.onClick.RemoveAllListeners();
            _generateMapButton.onClick.AddListener(GenerateMap);
            _randomSeedToggle.onValueChanged.RemoveAllListeners();
            _randomSeedToggle.onValueChanged.AddListener((isOn) => { _seedInputField.interactable = !isOn; });
            _seedInputField.text = _uiMapSystemManager.PlayerInputSeed.ToString();
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

        private void GenerateMap()
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;

            if (targetManager == null)
            {
                Debug.LogError("MapSystemManager reference is missing.");
                return;
            }

            if (_randomSeedToggle.isOn)
            {
                targetManager.UsePlayerInputSeed = false;
                targetManager.GenerateMap();
            }
            else
            {
                if (int.TryParse(_seedInputField.text, out int seed))
                {
                    targetManager.UsePlayerInputSeed = true;
                    targetManager.PlayerInputSeed = seed;
                    targetManager.GenerateMap();
                }
                else
                {
                    Debug.LogError("Invalid seed input. Please enter a valid integer.");
                }
            }

            _seedInputField.text = _uiMapSystemManager.PlayerInputSeed.ToString();
        }
    }
}