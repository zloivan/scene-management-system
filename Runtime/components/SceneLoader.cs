using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace IKhom.SceneManagementSystem.Runtime.data
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField]
        private Image _loadingBar;

        [SerializeField]
        private float _fillSpeed = 0.5f;

        [SerializeField]
        private Canvas _loadingCanvas;

        [SerializeField]
        private Camera _loadingCamera;

        [SerializeField]
        private SceneGroup[] _sceneGroups;

        [SerializeField]
        private bool _persistentBootstrap;

        [SerializeField]
        private bool _clearResourcesUnUnload;

        private float _targetProgress;
        private bool _isLoading;

        public SceneGroupManager Manager { get; private set; } //TODO Probably will be private

        private void Awake()
        {
            Manager = new SceneGroupManager(_persistentBootstrap, _clearResourcesUnUnload);

            Manager.OnSceneLoaded += sceneName => Debug.Log($"Loaded: {sceneName}");
            Manager.OnSceneUnloaded += sceneName => Debug.Log($"Unloaded: {sceneName}");
            Manager.OnSceneGroupLoaded += () => Debug.Log("Scene group loaded");
        }

        private async void Start()
        {
            await LoadSceneGroupAsync(0);
        }

        private void Update()
        {
            if (!_isLoading)
            {
                return;
            }

            var currentFillAmount = _loadingBar.fillAmount;
            var progressDifference = Mathf.Abs(currentFillAmount - _targetProgress);
            var dynamicFillSpeed = progressDifference * _fillSpeed;

            _loadingBar.fillAmount = Mathf.Lerp(currentFillAmount, _targetProgress, Time.deltaTime * dynamicFillSpeed);
        }

        public async Task LoadSceneGroupAsync(int index)
        {
            _loadingBar.fillAmount = 0f;
            _targetProgress = 1f;

            Debug.Assert(index >= 0 && index < _sceneGroups.Length,
                $"Invalid scene group index: {index}", this);

            var progress = new SceneLoadProgress();

            progress.Progressed += t => _targetProgress = Mathf.Max(t, _targetProgress);

            EnableLoadingCanvas();
            await Manager.LoadScenes(_sceneGroups[index], progress);
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