using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IKhom.SceneManagementSystem.Runtime.data
{
    public class SceneLoader : MonoBehaviour
    {
        public UnityEvent<string> OnSceneLoaded = new();
        public UnityEvent<string> OnSceneUnloaded = new();
        public UnityEvent OnSceneGroupLoaded = new();
        
        [Header("Loading Settings")]
        [SerializeField]
        private Image _loadingBar;

        [SerializeField]
        private Canvas _loadingCanvas;

        [SerializeField]
        private Camera _loadingCamera;

        [SerializeField]
        private float _fillSpeed = 0.5f;

        [SerializeField]
        private float _pauseAfterLoaded = 2f;

        [SerializeField]
        private float _jumpToEndTime;

        [Space]
        [Header("Scenes Loading Settings")]
        [SerializeField]
        private string _bootstrapSceneName;

        [SerializeField]
        private bool _loadFirstGroupOnStart;

        [SerializeField]
        private bool _persistentBootstrap;

        [SerializeField]
        private bool _clearResourcesUnUnload;

        [SerializeField]
        private SceneGroup[] _sceneGroups;

        public SceneGroup[] SceneGroups => _sceneGroups;

        private float _targetProgress;
        private bool _isLoading;
        private SceneGroupManager _manager;
        private bool _sceneIsLoaded;
        private float _increment;

        private void Awake()
        {
            _manager = string.IsNullOrEmpty(_bootstrapSceneName)
                ? new SceneGroupManager(_persistentBootstrap, _clearResourcesUnUnload)
                : new SceneGroupManager(_persistentBootstrap, _clearResourcesUnUnload, _bootstrapSceneName);

            _manager.OnSceneLoaded += sceneName => OnSceneLoaded?.Invoke(sceneName);
            _manager.OnSceneUnloaded += sceneName => OnSceneUnloaded?.Invoke(sceneName);
            _manager.OnSceneGroupLoaded += () => OnSceneGroupLoaded?.Invoke();
        }

        private async void Start()
        {
            if (_loadFirstGroupOnStart) await LoadSceneGroupAsync(0);
        }

        private void Update()
        {
            if (!_isLoading)
            {
                return;
            }

            if (_sceneIsLoaded)
            {
                _loadingBar.fillAmount = _jumpToEndTime == 0
                    ? _targetProgress
                    : Mathf.MoveTowards(_loadingBar.fillAmount, _targetProgress, Time.deltaTime * _increment);
            }
            else
            {
                var currentFillAmount = _loadingBar.fillAmount;
                var progressDifference = Mathf.Abs(currentFillAmount - _targetProgress);
                var dynamicFillSpeed = progressDifference * _fillSpeed;
                _loadingBar.fillAmount =
                    Mathf.Lerp(currentFillAmount, _targetProgress, Time.deltaTime * dynamicFillSpeed);
            }
        }

        public async Task LoadSceneGroupAsync(int index)
        {
            _loadingBar.fillAmount = 0f;
            _targetProgress = 1f;
            _sceneIsLoaded = false;
            Debug.Assert(index >= 0 && index < SceneGroups.Length, $"Invalid scene group index: {index}", this);

            var progress = new SceneLoadProgress();
            progress.Progressed += t => _targetProgress = Mathf.Max(t, _targetProgress);

            EnableLoadingCanvas();
            await _manager.LoadScenes(SceneGroups[index], progress);
            _sceneIsLoaded = true;

            if (_jumpToEndTime != 0)
            {
                _increment = Mathf.Abs(_targetProgress - _loadingBar.fillAmount) / _jumpToEndTime;
            }

            await Task.Delay(TimeSpan.FromSeconds(_pauseAfterLoaded + (_jumpToEndTime != 0 ? _jumpToEndTime : 0)));
            EnableLoadingCanvas(false);
        }

        private void EnableLoadingCanvas(bool enable = true)
        {
            _isLoading = enable;
            if (_loadingCanvas != null) _loadingCanvas.gameObject.SetActive(enable);
            if (_loadingCamera != null) _loadingCamera.gameObject.SetActive(enable);
        }
    }
}