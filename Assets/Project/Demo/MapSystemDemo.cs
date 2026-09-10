using PrimeTween;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BP.MapSystem
{
    public class MapSystemDemo : MonoBehaviour
    {
        [Header("Target Managers")]
        [Tooltip("The MapSystemManager driving the 2D UI Map.")]
        [SerializeField] private MapSystemManager _uiMapSystemManager;
        [Tooltip("The MapSystemManager driving the 3D World Map.")]
        [SerializeField] private MapSystemManager _3DMapSystemManager;

        [Header("Layout Panels")]
        [SerializeField] private GameObject _uiMapPanel;
        [SerializeField] private GameObject _3DMapPanel;

        [Header("Main Toggles")]
        [SerializeField] private Toggle _uiMapToggle;
        [SerializeField] private Toggle _3DMapToggle;
        [SerializeField] private Toggle _additionalSettingsToggle;

        [Header("Error Display")]
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private TweenSettings _errorDisplayTweenSettings;

        [Header("Generation Controls")]
        [SerializeField] private Button _generateMapButton;
        [SerializeField] private Toggle _randomSeedToggle;
        [SerializeField] private TMP_InputField _seedInputField;

        [Header("Settings Inputs")]
        [SerializeField] private TMP_InputField _maxAttemptsInputField;
        [SerializeField] private TMP_InputField _maxLevelsInputField;
        [SerializeField] private TMP_InputField _maxNodesPerLevelInputField;
        [SerializeField] private TMP_Dropdown _mapOrientationDropdown;
        [SerializeField] private TMP_InputField _nodeFacingDirectionInputField;
        [SerializeField] private TMP_InputField _uniquePathCountInputField;
        [SerializeField] private TMP_InputField _totalPathCountInputField;
        [SerializeField] private TMP_InputField _maxTraversalStepsInputField;

        [Header("Behavior Toggles")]
        [SerializeField] private Toggle _animateSpawnToggle;
        [SerializeField] private Toggle _animatePathTraversalToggle;
        [SerializeField] private Toggle _animatePathOnLoadToggle;
        [SerializeField] private Toggle _canVisitTravelledNodesToggle;

        [Header("Action Buttons")]
        [SerializeField] private Button _saveMapButton;
        [SerializeField] private Button _loadMapButton;
        [SerializeField] private Button _deleteSaveFileButton;
        [SerializeField] private Button _randomizeMapRotationButton;
        [SerializeField] private Button _resetMapRotationButton;

        [Header("Animation Durations")]
        [SerializeField] private float _nodeSpawnAnimationDuration = 0.5f;
        [SerializeField] private float _pathSpawnAnimationDuration = 0.5f;
        [SerializeField] private float _pathTraversalAnimationDuration = 0.5f;
        [SerializeField] private float _pathAnimationOnLoadDuration = 0.5f;

        private Sequence _errorSequence;
        private Tween _rotationTween;
        private Vector3 _uiMapInitialRotation;
        private Vector3 _3DMapInitialRotation;

        #region Unity API

        private void Awake()
        {
            PrimeTweenConfig.SetTweensCapacity(400);

            _uiMapInitialRotation = _uiMapSystemManager.MapContainer.localEulerAngles;
            _3DMapInitialRotation = _3DMapSystemManager.MapContainer.localEulerAngles;

            _uiMapPanel.SetActive(false);
            _3DMapPanel.SetActive(false);
            _errorText.gameObject.SetActive(false);

            RebindUIEvents();
        }

        private void Start()
        {
            _uiMapToggle.isOn = true;
            OnUIToggleChanged(_uiMapToggle.isOn);
        }

        #endregion Unity API

        #region Initialization

        private void RebindUIEvents()
        {
            _uiMapToggle.onValueChanged.AddListener(OnUIToggleChanged);
            _3DMapToggle.onValueChanged.AddListener(On3DToggleChanged);
            _additionalSettingsToggle.onValueChanged.AddListener(OnAdditionalSettingsToggleChanged);

            _generateMapButton.onClick.AddListener(GenerateMap);
            _randomSeedToggle.onValueChanged.AddListener(isOn => _seedInputField.interactable = !isOn);
            _seedInputField.text = _uiMapSystemManager.PlayerInputSeed.ToString();

            _maxAttemptsInputField.onEndEdit.AddListener(EditMaxAttempts);
            _maxLevelsInputField.onEndEdit.AddListener(EditMaxLevels);
            _maxNodesPerLevelInputField.onEndEdit.AddListener(EditMaxNodesPerLevel);
            _mapOrientationDropdown.onValueChanged.AddListener(EditMapOrientation);
            _nodeFacingDirectionInputField.onEndEdit.AddListener(EditNodeFacingDirection);

            _animateSpawnToggle.onValueChanged.AddListener(EditAnimateSpawn);
            _animatePathOnLoadToggle.onValueChanged.AddListener(EditAnimatePathOnLoad);
            _animatePathTraversalToggle.onValueChanged.AddListener(EditAnimatePathTraversal);

            _uniquePathCountInputField.onEndEdit.AddListener(EditUniquePathCount);
            _totalPathCountInputField.onEndEdit.AddListener(EditTotalPathCount);
            _maxTraversalStepsInputField.onEndEdit.AddListener(EditMaxTraversalSteps);
            _canVisitTravelledNodesToggle.onValueChanged.AddListener(EditCanVisitTravelledNodes);

            _saveMapButton.onClick.AddListener(OnSaveMapButtonClicked);
            _loadMapButton.onClick.AddListener(OnLoadMapButtonClicked);
            _deleteSaveFileButton.onClick.AddListener(OnDeleteSaveFileButtonClicked);
            _randomizeMapRotationButton.onClick.AddListener(OnRandomizeMapRotationButtonClicked);
            _resetMapRotationButton.onClick.AddListener(OnResetMapRotationButtonClicked);
        }

        #endregion Initialization

        #region Core Actions

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
            targetManager.DeleteMapSave();
        }

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

        #endregion Core Actions

        #region UI Callbacks

        private void OnUIToggleChanged(bool isOn)
        {
            if (!isOn) return;

            _uiMapPanel.SetActive(true);
            _3DMapPanel.SetActive(false);

            RefreshUIFromManager(_uiMapSystemManager);
        }

        private void On3DToggleChanged(bool isOn)
        {
            if (!isOn) return;

            _uiMapPanel.SetActive(false);
            _3DMapPanel.SetActive(true);

            RefreshUIFromManager(_3DMapSystemManager);
        }

        private void OnAdditionalSettingsToggleChanged(bool isOn)
        {
            if (_uiMapToggle.isOn)
                OnUIToggleChanged(true);
            else if (_3DMapToggle.isOn)
                On3DToggleChanged(true);
        }

        private void RefreshUIFromManager(MapSystemManager manager)
        {
            _maxAttemptsInputField.text = manager.GenerationAttempts.ToString();

            if (manager.TryGetComponent(out MapNodeGenerator nodeGenerator))
            {
                _maxLevelsInputField.text = nodeGenerator.MaxLevels.ToString();
                _maxNodesPerLevelInputField.text = nodeGenerator.NodesPerLevel.ToString();

                _mapOrientationDropdown.ClearOptions();
                _mapOrientationDropdown.AddOptions(System.Enum.GetNames(typeof(MapDirection)).ToList());
                _mapOrientationDropdown.value = (int)nodeGenerator.Direction;

                _nodeFacingDirectionInputField.text = nodeGenerator.NodeFacingDirection.ToString();
                _animateSpawnToggle.isOn = nodeGenerator.NodeSpawnAnimationDuration > .001f;
            }

            if (manager.TryGetComponent(out MapTraversalController traversalController))
            {
                _animatePathOnLoadToggle.isOn = traversalController.PathTraversalAnimationOnLoadDuration > .001f;
                _animatePathTraversalToggle.isOn = traversalController.PathTraversalAnimationDuration > .001f;
                _maxTraversalStepsInputField.text = traversalController.MaxTraversalSteps.ToString();
                _canVisitTravelledNodesToggle.isOn = traversalController.CanTraverseVisitedNodes;
            }

            if (manager.TryGetComponent(out MapPathGenerator pathGenerator))
            {
                _uniquePathCountInputField.text = pathGenerator.UniquePaths.ToString();
                _totalPathCountInputField.text = pathGenerator.TotalPaths.ToString();
            }
        }

        #endregion UI Callbacks

        #region Additional Settings Callbacks

        private void EditMaxAttempts(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (int.TryParse(input, out int maxAttempts)) targetManager.GenerationAttempts = maxAttempts;
            else DisplayError("Invalid max attempts input. Please enter a valid integer.");
        }

        private void EditMaxLevels(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (int.TryParse(input, out int maxLevels))
            {
                if (targetManager.TryGetComponent(out MapNodeGenerator gen)) gen.MaxLevels = maxLevels;
            }
            else DisplayError("Invalid max levels input. Please enter a valid integer.");
        }

        private void EditMaxNodesPerLevel(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (int.TryParse(input, out int maxNodesPerLevel))
            {
                if (targetManager.TryGetComponent(out MapNodeGenerator gen)) gen.NodesPerLevel = maxNodesPerLevel;
            }
            else DisplayError("Invalid max nodes per level input. Please enter a valid integer.");
        }

        private void EditMapOrientation(int index)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (targetManager.TryGetComponent(out MapNodeGenerator gen))
            {
                if (System.Enum.IsDefined(typeof(MapDirection), index)) gen.Direction = (MapDirection)index;
                else DisplayError("Invalid map orientation selection.");
            }
        }

        private void EditNodeFacingDirection(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (int.TryParse(input, out int zRotation) && targetManager.TryGetComponent(out MapNodeGenerator gen))
            {
                gen.NodeFacingDirection = zRotation;
            }
        }

        private void EditAnimateSpawn(bool isOn)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (targetManager.TryGetComponent(out MapNodeGenerator nodeGen))
                nodeGen.NodeSpawnAnimationDuration = isOn ? _nodeSpawnAnimationDuration : .0001f;
            if (targetManager.TryGetComponent(out MapPathGenerator pathGen))
                pathGen.PathSpawnAnimationDuration = isOn ? _pathSpawnAnimationDuration : .0001f;
        }

        private void EditAnimatePathOnLoad(bool isOn)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (targetManager.TryGetComponent(out MapTraversalController controller))
                controller.PathTraversalAnimationOnLoadDuration = isOn ? _pathAnimationOnLoadDuration : .0001f;
        }

        private void EditAnimatePathTraversal(bool isOn)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (targetManager.TryGetComponent(out MapTraversalController controller))
                controller.PathTraversalAnimationDuration = isOn ? _pathTraversalAnimationDuration : .0001f;
        }

        private void EditUniquePathCount(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (int.TryParse(input, out int uniquePaths) && targetManager.TryGetComponent(out MapPathGenerator gen))
            {
                gen.UniquePaths = uniquePaths;
            }
        }

        private void EditTotalPathCount(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (int.TryParse(input, out int totalPaths) && targetManager.TryGetComponent(out MapPathGenerator gen))
            {
                gen.TotalPaths = totalPaths;
            }
        }

        private void EditMaxTraversalSteps(string input)
        {
            var targetManager = _uiMapToggle.isOn ? _uiMapSystemManager : _3DMapSystemManager;
            if (int.TryParse(input, out int maxSteps) && targetManager.TryGetComponent(out MapTraversalController controller))
            {
                controller.MaxTraversalSteps = maxSteps;
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

        #endregion Additional Settings Callbacks

        #region Error Handling

        private void DisplayError(string msg, float duration = 3f)
        {
            if (_errorText == null) return;

            _errorSequence.Stop();

            _errorText.text = msg;
            _errorText.alpha = 1f;
            _errorText.transform.localScale = Vector3.zero;
            _errorText.gameObject.SetActive(true);

#pragma warning disable CS0618 // Type or member is obsolete
            _errorSequence = Sequence.Create()
                .Chain(Tween.Scale(_errorText.transform, Vector3.one, _errorDisplayTweenSettings))
                .Chain(Tween.Delay(duration))
                .Chain(Tween.Alpha(_errorText, 0f, _errorDisplayTweenSettings))
                .OnComplete(() => _errorText.gameObject.SetActive(false));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        #endregion Error Handling
    }
}