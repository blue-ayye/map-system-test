using PrimeTween;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BP.MapSystem
{
    public class MapSystemDemo : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapSystemManager _uiMapSystemManager;
        [SerializeField] private MapSystemManager _3DMapSystemManager;
        [SerializeField] private GameObject _uiMapPanel;
        [SerializeField] private GameObject _3DMapPanel;
        [SerializeField] private Toggle _uiMapToggle;
        [SerializeField] private Toggle _3DMapToggle;
        [SerializeField] private Toggle _additionalSettingsToggle;

        [Header("Error Display")]
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private TweenSettings _errorDisplayTweenSettings;

        [Header("Map Generation UI")]
        [SerializeField] private Button _generateMapButton;
        [SerializeField] private Toggle _randomSeedToggle;
        [SerializeField] private TMP_InputField _seedInputField;

        [Header("Additional Settings")]
        [SerializeField] private TMP_InputField _maxAttemptsInputField;
        [SerializeField] private TMP_InputField _maxLevelsInputField;
        [SerializeField] private TMP_InputField _maxNodesPerLevelInputField;
        [SerializeField] private TMP_Dropdown _mapOrientationDropdown;
        [SerializeField] private TMP_InputField _nodeFacingDirectionInputField;
        [SerializeField] private Toggle _animateSpawnToggle;
        [SerializeField] private Toggle _animateLoadingToggle;
        [SerializeField] private TMP_InputField _uniquePathCountInputField;
        [SerializeField] private TMP_InputField _totalPathCountInputField;
        [SerializeField] private Button _saveMapButton;
        [SerializeField] private Button _loadMapButton;
        [SerializeField] private Button _deleteSaveFileButton;
        [SerializeField] private Button _randomizeMapRotationButton;
        [SerializeField] private Button _resetMapRotationButton;

        private Tween _errorScaleTween;
        private Tween _errorAlphaTween;

        private void Awake()
        {
            _uiMapPanel.SetActive(false);
            _3DMapPanel.SetActive(false);

            _uiMapToggle.onValueChanged.RemoveAllListeners();
            _3DMapToggle.onValueChanged.RemoveAllListeners();
            _additionalSettingsToggle.onValueChanged.RemoveAllListeners();
            _uiMapToggle.onValueChanged.AddListener(OnUIToggleChanged);
            _3DMapToggle.onValueChanged.AddListener(On3DToggleChanged);
            _additionalSettingsToggle.onValueChanged.AddListener(OnAdditionalSettingsToggleChanged);

            _generateMapButton.onClick.RemoveAllListeners();
            _generateMapButton.onClick.AddListener(GenerateMap);
            _randomSeedToggle.onValueChanged.RemoveAllListeners();
            _randomSeedToggle.onValueChanged.AddListener((isOn) => { _seedInputField.interactable = !isOn; });
            _seedInputField.text = _uiMapSystemManager.PlayerInputSeed.ToString();

            _errorText.gameObject.SetActive(false);

            _maxAttemptsInputField.onEndEdit.RemoveAllListeners();
            _maxAttemptsInputField.onEndEdit.AddListener(EditMaxAttempts);

            _maxLevelsInputField.onEndEdit.RemoveAllListeners();
            _maxLevelsInputField.onEndEdit.AddListener(EditMaxLevels);

            _maxNodesPerLevelInputField.onEndEdit.RemoveAllListeners();
            _maxNodesPerLevelInputField.onEndEdit.AddListener(EditMaxNodesPerLevel);

            _mapOrientationDropdown.onValueChanged.RemoveAllListeners();
            _mapOrientationDropdown.onValueChanged.AddListener(EditMapOrientation);

            //_nodeFacingDirectionInputField.text = _uiMapSystemManager.NodeFacingDirection.ToString();
            //_nodeFacingDirectionInputField.onEndEdit.RemoveAllListeners();
            //_nodeFacingDirectionInputField.onEndEdit.AddListener(EditNodeFacingDirection);
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

                _maxAttemptsInputField.text = _uiMapSystemManager.GenerationAttempts.ToString();

                if (_uiMapSystemManager.TryGetComponent(out MapNodeGenerator nodeGenerator))
                {
                    _maxLevelsInputField.text = nodeGenerator.MaxLevels.ToString();
                    _maxNodesPerLevelInputField.text = nodeGenerator.NodesPerLevel.ToString();

                    // Populate the dropdown with the enum values
                    _mapOrientationDropdown.ClearOptions();
                    _mapOrientationDropdown.AddOptions(System.Enum.GetNames(typeof(MapDirection)).ToList());
                    _mapOrientationDropdown.value = (int)nodeGenerator.Direction;
                }
            }
        }

        private void On3DToggleChanged(bool isOn)
        {
            if (isOn)
            {
                _uiMapPanel.SetActive(false);
                _3DMapPanel.SetActive(true);

                _maxAttemptsInputField.text = _3DMapSystemManager.GenerationAttempts.ToString();

                if (_3DMapSystemManager.TryGetComponent(out MapNodeGenerator nodeGenerator3D))
                {
                    _maxLevelsInputField.text = nodeGenerator3D.MaxLevels.ToString();
                    _maxNodesPerLevelInputField.text = nodeGenerator3D.NodesPerLevel.ToString();

                    _mapOrientationDropdown.ClearOptions();
                    _mapOrientationDropdown.AddOptions(System.Enum.GetNames(typeof(MapDirection)).ToList());
                    _mapOrientationDropdown.value = (int)nodeGenerator3D.Direction;
                }
            }
        }

        private void OnAdditionalSettingsToggleChanged(bool isOn)
        {
            // Just a hack to update the settings
            if (_uiMapToggle.isOn)
                OnUIToggleChanged(_uiMapToggle.isOn);
            else if (_3DMapToggle.isOn)
                On3DToggleChanged(_3DMapToggle.isOn);
        }

        private void GenerateMap()
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;

            if (targetManager == null)
            {
                DisplayError("MapSystemManager reference is missing. Please assign it in the inspector.");
                return;
            }

            if (_randomSeedToggle.isOn)
            {
                targetManager.UsePlayerInputSeed = false;
                targetManager.GenerateMap();

                _seedInputField.text = targetManager.GeneratedSeed.ToString();
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
                    DisplayError("Invalid seed input. Please enter a valid integer.");
                }
            }
        }

        private void EditMaxAttempts(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;

            if (int.TryParse(input, out int maxAttempts))
            {
                targetManager.GenerationAttempts = maxAttempts;
            }
            else
            {
                DisplayError("Invalid max attempts input. Please enter a valid integer.");
            }
        }

        private void EditMaxNodesPerLevel(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;

            if (int.TryParse(input, out int maxNodesPerLevel))
            {
                if (targetManager.TryGetComponent(out MapNodeGenerator nodeGenerator))
                    nodeGenerator.NodesPerLevel = maxNodesPerLevel;
            }
            else
            {
                DisplayError("Invalid max nodes per level input. Please enter a valid integer.");
            }
        }

        private void EditMapOrientation(int index)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (targetManager.TryGetComponent(out MapNodeGenerator nodeGenerator))
            {
                if (System.Enum.IsDefined(typeof(MapDirection), index))
                {
                    nodeGenerator.Direction = (MapDirection)index;
                }
                else
                {
                    DisplayError("Invalid map orientation selection.");
                }
            }
        }

        private void EditMaxLevels(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;

            if (int.TryParse(input, out int maxLevels))
            {
                if (targetManager.TryGetComponent(out MapNodeGenerator nodeGenerator))
                    nodeGenerator.MaxLevels = maxLevels;
            }
            else
            {
                DisplayError("Invalid max levels input. Please enter a valid integer.");
            }
        }

        private void DisplayError(string msg, float duration = 3f)
        {
            if (_errorText != null)
            {
                _errorScaleTween.Stop();
                _errorAlphaTween.Stop();

                _errorText.text = msg;
                _errorText.alpha = 1f;
                _errorText.transform.localScale = Vector3.zero;
                _errorText.gameObject.SetActive(true);
                _errorScaleTween = Tween.Scale(_errorText.transform, Vector3.one, _errorDisplayTweenSettings).OnComplete(() =>
                 {
                     Tween.Delay(duration, onComplete: () =>
                     {
                         _errorAlphaTween = Tween.Alpha(_errorText, 0f, _errorDisplayTweenSettings).OnComplete(() =>
                                {
                                    _errorText.gameObject.SetActive(false);
                                });
                     });
                 });
            }
        }
    }
}