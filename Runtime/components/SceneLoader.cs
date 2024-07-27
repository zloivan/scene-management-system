using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace IKhom.SceneManagementSystem.Runtime.data
{
    public class SceneLoader : MonoBehaviour
    {
        [Header("Loading Settings")]
        [SerializeField] private Image _loadingBar;
        [SerializeField] private Canvas _loadingCanvas;
        [SerializeField] private Camera _loadingCamera;
        [SerializeField] private float _fillSpeed = 0.5f;
        [SerializeField] private float _minLoadingTime = 10f;
        [SerializeField] private float _pauseAfterLoaded = 2f;
        [Space]
        [Header("Scenes Loading Settings")]
        [SerializeField] private SceneGroup[] _sceneGroups;
        [SerializeField] private bool _persistentBootstrap;
        [SerializeField] private bool _clearResourcesUnUnload;


        public SceneGroup[] SceneGroups => _sceneGroups;
        
        
        private float _targetProgress;
        private bool _isLoading;
        private float _elapsedTime;
        private bool _scenesLoaded;
        private SceneGroupManager _manager;


        private void Awake()
        {
            _manager = new SceneGroupManager(_persistentBootstrap, _clearResourcesUnUnload);
            _manager.OnSceneLoaded += sceneName =>
            {
                Debug.Log($"Loaded: {sceneName}");
            };
            _manager.OnSceneUnloaded += sceneName => Debug.Log($"Unloaded: {sceneName}");
            _manager.OnSceneGroupLoaded += () =>
            {
                _scenesLoaded = true;
            };
        }

        private async void Start()
        {
            await LoadSceneGroupAsync(0);
        }

        private bool _timeAdjusted;
        private float _dynamicFillSpeed;

        private void Update()
        {
           
            if (!_isLoading)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;
            
            var currentFillAmount =_loadingBar.fillAmount;
            var progressDifference = Mathf.Abs(currentFillAmount - _targetProgress);

            if (!_scenesLoaded)
            {
                _dynamicFillSpeed = progressDifference * _fillSpeed;
                _loadingBar.fillAmount = Mathf.Lerp(currentFillAmount, _targetProgress, Time.deltaTime * _dynamicFillSpeed);
            }
            else
            {
                if (!_timeAdjusted)
                {
                    var remainingTime = _minLoadingTime - _elapsedTime;
                    _dynamicFillSpeed = progressDifference / remainingTime;
                    _timeAdjusted = true;
                }

                _loadingBar.fillAmount =
                    Mathf.MoveTowards(currentFillAmount, _targetProgress, Time.deltaTime * _dynamicFillSpeed);
            }
        }

        public async Task LoadSceneGroupAsync(int index)
        {
            _loadingBar.fillAmount = 0f;
            _targetProgress = 1f;
            _elapsedTime = 0f;
            _scenesLoaded = false;

            Debug.Assert(index >= 0 && index < SceneGroups.Length,
                $"Invalid scene group index: {index}", this);

            var progress = new SceneLoadProgress();

            progress.Progressed += t => _targetProgress = Mathf.Max(t, _targetProgress);

            EnableLoadingCanvas();
            var loadingTask = _manager.LoadScenes(SceneGroups[index], progress);
            //Wait for that, if elapsed time < min time wait for difference
            //else if elapsed time more then min => set progress to 100
            
            
            var minTimeTask = Task.Delay(TimeSpan.FromSeconds(_minLoadingTime));

            await Task.WhenAll(loadingTask, minTimeTask);

            if (_elapsedTime >= _minLoadingTime)
            {
                _loadingBar.fillAmount = 1f;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(_pauseAfterLoaded));
            
            EnableLoadingCanvas(false);
        }

        private void EnableLoadingCanvas(bool enable = true)
        {
            _isLoading = enable;
            _loadingCanvas.gameObject.SetActive(enable);
            _loadingCamera.gameObject.SetActive(enable);
        }
    }
}