using PrimeTween;
using System;
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
            _uiMapToggle.onValueChanged.AddListener(OnUIToggleChanged);
            _3DMapToggle.onValueChanged.AddListener(On3DToggleChanged);

            _generateMapButton.onClick.RemoveAllListeners();
            _generateMapButton.onClick.AddListener(GenerateMap);
            _randomSeedToggle.onValueChanged.RemoveAllListeners();
            _randomSeedToggle.onValueChanged.AddListener((isOn) => { _seedInputField.interactable = !isOn; });
            _seedInputField.text = _uiMapSystemManager.PlayerInputSeed.ToString();

            _errorText.gameObject.SetActive(false);

            _maxAttemptsInputField.text = _uiMapSystemManager.GenerationAttempts.ToString();
            _maxAttemptsInputField.onEndEdit.RemoveAllListeners();
            _maxAttemptsInputField.onEndEdit.AddListener(EditMaxAttempts);

            _maxLevelsInputField.text = _uiMapSystemManager.MapLevels.ToString();
            _maxLevelsInputField.onEndEdit.RemoveAllListeners();
            _maxLevelsInputField.onEndEdit.AddListener(EditMaxLevels);

            _maxNodesPerLevelInputField.text = _uiMapSystemManager.NodesPerLevel.ToString();
            _maxNodesPerLevelInputField.onEndEdit.RemoveAllListeners();
            _maxNodesPerLevelInputField.onEndEdit.AddListener(EditMaxNodesPerLevel);

            //// Populate the dropdown with the enum values
            //_mapOrientationDropdown.ClearOptions();
            //_mapOrientationDropdown.AddOptions(System.Enum.GetNames(typeof(MapOrientation)).ToList());
            //_mapOrientationDropdown.value = (int)_uiMapSystemManager.MapOrientation;
            //_mapOrientationDropdown.onValueChanged.RemoveAllListeners();
            //_mapOrientationDropdown.onValueChanged.AddListener(EditMapOrientation);

            //_nodeFacingDirectionInputField.text = _uiMapSystemManager.NodeFacingDirection.ToString();
            //_nodeFacingDirectionInputField.onEndEdit.RemoveAllListeners();
            //_nodeFacingDirectionInputField.onEndEdit.AddListener(EditNodeFacingDirection);
        }

        private void EditMaxNodesPerLevel(string input)
        {
            if (int.TryParse(input, out int maxNodesPerLevel))
            {
                _uiMapSystemManager.NodesPerLevel = maxNodesPerLevel;
                _3DMapSystemManager.NodesPerLevel = maxNodesPerLevel;
            }
            else
            {
                DisplayError("Invalid max nodes per level input. Please enter a valid integer.");
            }
        }

        private void EditMaxLevels(string input)
        {
            if (int.TryParse(input, out int maxLevels))
            {
                _uiMapSystemManager.MapLevels = maxLevels;
                _3DMapSystemManager.MapLevels = maxLevels;
            }
            else
            {
                DisplayError("Invalid max levels input. Please enter a valid integer.");
            }
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
            if (int.TryParse(input, out int maxAttempts))
            {
                _uiMapSystemManager.GenerationAttempts = maxAttempts;
                _3DMapSystemManager.GenerationAttempts = maxAttempts;
            }
            else
            {
                DisplayError("Invalid max attempts input. Please enter a valid integer.");
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