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
        [SerializeField] private Toggle _animatePathTraversalToggle;
        [SerializeField] private Toggle _animatePathOnLoadToggle;
        [SerializeField] private TMP_InputField _uniquePathCountInputField;
        [SerializeField] private TMP_InputField _totalPathCountInputField;
        [SerializeField] private TMP_InputField _maxTraversalStepsInputField;
        [SerializeField] private Toggle _canVisitTravelledNodesToggle;
        [SerializeField] private Button _saveMapButton;
        [SerializeField] private Button _loadMapButton;
        [SerializeField] private Button _deleteSaveFileButton;
        [SerializeField] private Button _randomizeMapRotationButton;
        [SerializeField] private Button _resetMapRotationButton;

        [SerializeField] private float _nodeSpawnAnimationDuration = 0.5f;
        [SerializeField] private float _pathSpawnAnimationDuration = 0.5f;
        [SerializeField] private float _pathTraversalAnimationDuration = 0.5f;
        [SerializeField] private float _pathAnimationOnLoadDuration = 0.5f;

        private Tween _errorScaleTween;
        private Tween _errorAlphaTween;

        private void Awake()
        {
            _uiMapInitialRotation = _uiMapSystemManager.MapContainer.localEulerAngles;
            _3DMapInitialRotation = _3DMapSystemManager.MapContainer.localEulerAngles;

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

            _nodeFacingDirectionInputField.onEndEdit.RemoveAllListeners();
            _nodeFacingDirectionInputField.onEndEdit.AddListener(EditNodeFacingDirection);

            _animateSpawnToggle.onValueChanged.RemoveAllListeners();
            _animateSpawnToggle.onValueChanged.AddListener(EditAnimateSpawn);

            _animatePathOnLoadToggle.onValueChanged.RemoveAllListeners();
            _animatePathOnLoadToggle.onValueChanged.AddListener(EditAnimatePathOnLoad);

            _animatePathTraversalToggle.onValueChanged.RemoveAllListeners();
            _animatePathTraversalToggle.onValueChanged.AddListener(EditAnimatePathTraversal);

            _uniquePathCountInputField.onEndEdit.RemoveAllListeners();
            _uniquePathCountInputField.onEndEdit.AddListener(EditUniquePathCount);

            _totalPathCountInputField.onEndEdit.RemoveAllListeners();
            _totalPathCountInputField.onEndEdit.AddListener(EditTotalPathCount);

            _maxTraversalStepsInputField.onEndEdit.RemoveAllListeners();
            _maxTraversalStepsInputField.onEndEdit.AddListener(EditMaxTraversalSteps);

            _canVisitTravelledNodesToggle.onValueChanged.RemoveAllListeners();
            _canVisitTravelledNodesToggle.onValueChanged.AddListener(EditCanVisitTravelledNodes);

            _saveMapButton.onClick.RemoveAllListeners();
            _saveMapButton.onClick.AddListener(OnSaveMapButtonClicked);

            _loadMapButton.onClick.RemoveAllListeners();
            _loadMapButton.onClick.AddListener(OnLoadMapButtonClicked);

            _deleteSaveFileButton.onClick.RemoveAllListeners();
            _deleteSaveFileButton.onClick.AddListener(OnDeleteSaveFileButtonClicked);

            _randomizeMapRotationButton.onClick.RemoveAllListeners();
            _randomizeMapRotationButton.onClick.AddListener(OnRandomizeMapRotationButtonClicked);

            _resetMapRotationButton.onClick.RemoveAllListeners();
            _resetMapRotationButton.onClick.AddListener(OnResetMapRotationButtonClicked);
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

                    _nodeFacingDirectionInputField.text = nodeGenerator.NodeFacingDirection.ToString();

                    _animateSpawnToggle.isOn = nodeGenerator.NodeSpawnAnimationDuration > .001f;
                }

                if (_uiMapSystemManager.TryGetComponent(out MapTraversalController traversalController))
                {
                    _animatePathOnLoadToggle.isOn = traversalController.PathTraversalAnimationOnLoadDuration > .001f;
                    _animatePathTraversalToggle.isOn = traversalController.PathTraversalAnimationDuration > .001f;
                    _maxTraversalStepsInputField.text = traversalController.MaxTraversalSteps.ToString();
                    _canVisitTravelledNodesToggle.isOn = traversalController.CanTraverseVisitedNodes;
                }

                if (_uiMapSystemManager.TryGetComponent(out MapPathGenerator pathGenerator))
                {
                    _uniquePathCountInputField.text = pathGenerator.UniquePaths.ToString();
                    _totalPathCountInputField.text = pathGenerator.TotalPaths.ToString();
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

                    _nodeFacingDirectionInputField.text = nodeGenerator3D.NodeFacingDirection.ToString();

                    _animateSpawnToggle.isOn = nodeGenerator3D.NodeSpawnAnimationDuration > .001f;
                }

                if (_3DMapSystemManager.TryGetComponent(out MapTraversalController traversalController3D))
                {
                    _animatePathOnLoadToggle.isOn = traversalController3D.PathTraversalAnimationOnLoadDuration > .001f;
                    _animatePathTraversalToggle.isOn = traversalController3D.PathTraversalAnimationDuration > .001f;
                    _maxTraversalStepsInputField.text = traversalController3D.MaxTraversalSteps.ToString();
                    _canVisitTravelledNodesToggle.isOn = traversalController3D.CanTraverseVisitedNodes;
                }

                if (_3DMapSystemManager.TryGetComponent(out MapPathGenerator pathGenerator3D))
                {
                    _uniquePathCountInputField.text = pathGenerator3D.UniquePaths.ToString();
                    _totalPathCountInputField.text = pathGenerator3D.TotalPaths.ToString();
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

        private void EditNodeFacingDirection(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (int.TryParse(input, out int zRotation))
            {
                if (targetManager.TryGetComponent(out MapNodeGenerator nodeGenerator))
                {
                    nodeGenerator.NodeFacingDirection = zRotation;
                }
            }
        }

        private void EditAnimateSpawn(bool isOn)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (targetManager.TryGetComponent(out MapNodeGenerator nodeGenerator))
            {
                nodeGenerator.NodeSpawnAnimationDuration = isOn ? _nodeSpawnAnimationDuration : .0001f;
            }

            if (targetManager.TryGetComponent(out MapPathGenerator pathGenerator))
            {
                pathGenerator.PathSpawnAnimationDuration = isOn ? _pathSpawnAnimationDuration : .0001f;
            }
        }

        private void EditAnimatePathOnLoad(bool isOn)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (targetManager.TryGetComponent(out MapTraversalController controller))
            {
                controller.PathTraversalAnimationOnLoadDuration = isOn ? _pathAnimationOnLoadDuration : .0001f;
            }
        }

        private void EditAnimatePathTraversal(bool isOn)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (targetManager.TryGetComponent(out MapTraversalController controller))
            {
                controller.PathTraversalAnimationDuration = isOn ? _pathTraversalAnimationDuration : .0001f;
            }
        }

        private void EditUniquePathCount(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (int.TryParse(input, out int uniquePathCount))
            {
                if (targetManager.TryGetComponent(out MapPathGenerator pathGenerator))
                {
                    pathGenerator.UniquePaths = uniquePathCount;
                }
            }
        }

        private void EditTotalPathCount(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (int.TryParse(input, out int totalPathCount))
            {
                if (targetManager.TryGetComponent(out MapPathGenerator pathGenerator))
                {
                    pathGenerator.TotalPaths = totalPathCount;
                }
            }
        }

        private void EditMaxTraversalSteps(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (int.TryParse(input, out int maxTraversalSteps))
            {
                if (targetManager.TryGetComponent(out MapTraversalController controller))
                {
                    controller.MaxTraversalSteps = maxTraversalSteps;
                }
            }
        }

        private void EditCanVisitTravelledNodes(bool isOn)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (targetManager.TryGetComponent(out MapTraversalController controller))
            {
                controller.CanTraverseVisitedNodes = isOn;
            }
        }

        private void OnSaveMapButtonClicked()
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            targetManager.SaveMap();
        }

        private void OnLoadMapButtonClicked()
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            targetManager.LoadMap();
        }

        private void OnDeleteSaveFileButtonClicked()
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            targetManager.DeleteSave();
        }

        private Tween _rotationTween;
        private Vector3 _uiMapInitialRotation;
        private Vector3 _3DMapInitialRotation;

        private void OnRandomizeMapRotationButtonClicked()
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (targetManager.MapContainer != null)
            {
                var randomRotation = Quaternion.Euler(Random.Range(-30f, 30f), Random.Range(0f, 360f), Random.Range(-30f, 30f));

                _rotationTween.Stop();
                _rotationTween = Tween.Rotation(targetManager.MapContainer, randomRotation, 5f);
            }
        }

        private void OnResetMapRotationButtonClicked()
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (targetManager.MapContainer != null)
            {
                _rotationTween.Stop();
                _rotationTween = Tween.Rotation(targetManager.MapContainer, Quaternion.Euler(targetManager == _uiMapSystemManager ? _uiMapInitialRotation : _3DMapInitialRotation), 5f);
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
#pragma warning disable CS0618
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
#pragma warning restore CS0618
            }
        }
    }
}