using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

#if SCENE_MANAGEMENT_ADDRESSABLES_ENABLED
using UnityEngine.AddressableAssets;
#endif

namespace IKhom.SceneManagementSystem.Runtime.data
{
    public class SceneGroupManager
    {
        public event Action<string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnloaded = delegate { };
        public event Action OnSceneGroupLoaded = delegate { };

        private readonly bool _isBootstrapPersistentScene;
        private readonly bool _unloadRecoursesWithScenes;
        private readonly string _bootstrapper;
        private readonly ILogger _logger = new SceneManagerLogger();
#if SCENE_MANAGEMENT_ADDRESSABLES_ENABLED
        private readonly AsyncOperationHandleGroup _handleGroup = new(10);
#endif
        public SceneGroup ActiveSceneGroup { get; private set; }

        public SceneGroupManager(bool isBootstrapPersistent,
            bool unloadRecoursesWithScenes,
            string bootstrapSceneName = "Bootstrapper")
        {
            _isBootstrapPersistentScene = isBootstrapPersistent;
            _unloadRecoursesWithScenes = unloadRecoursesWithScenes;
            _bootstrapper = bootstrapSceneName;

            _logger.Log(
                $"SceneGroupManager is initialized, with isBootstrapPersistent: {isBootstrapPersistent}, unloadRecoursesWithScenes: {unloadRecoursesWithScenes} and bootstrapSceneName {bootstrapSceneName}");
        }

        public async Task LoadScenes(SceneGroup group, IProgress<float> progress, bool reloadDupScenes = false)
        {
            _logger.Log("Start loading scene group...");
            ActiveSceneGroup = group;

            var loadedScenes = new List<string>();

            await UnloadScenes();

            var sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }

            var totalScenesToLoad = ActiveSceneGroup.Scenes.Count;

            _logger.Log($"About to load {totalScenesToLoad} scenes...");
            var operationGroup = new AsyncOperationGroup(totalScenesToLoad);

            for (var i = 0; i < totalScenesToLoad; i++)
            {
                var sceneData = group.Scenes[i];

                if (reloadDupScenes == false && loadedScenes.Contains(sceneData.Name))
                {
                    _logger.LogWarning("Not Critical", "Reload scenes is disables, loading next scene...");
                    continue;
                }

                if (sceneData.Reference.State == SceneReferenceState.Regular)
                {
                    var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
                    await Task.Delay(2500);
                    operationGroup.Operations.Add(operation);
                }

#if SCENE_MANAGEMENT_ADDRESSABLES_ENABLED
                else if (sceneData.Reference.State == SceneReferenceState.Addressable)
                {
                    var sceneHandle = Addressables.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
                    _handleGroup.Handles.Add(sceneHandle);
                }
#endif
                OnSceneLoaded?.Invoke(sceneData.Name);
                _logger.Log($"Loaded {sceneData.Name}");
            }


#if SCENE_MANAGEMENT_ADDRESSABLES_ENABLED
            //Wait until all AsyncOperations in the group are loaded
            while (!operationGroup.IsDone || !_handleGroup.IsDone)
            {
                progress?.Report((operationGroup.Progress + _handleGroup.Progress) / 2);
                await Task.Delay(100); //don't overload the progress
            }
#else
            while (!operationGroup.IsDone)
            {
                progress?.Report(operationGroup.Progress);
                await Task.Delay(100); //don't overload the progress
            }
#endif
            


            var activeScene =
                SceneManager.GetSceneByName(ActiveSceneGroup.FindSceneNameByType(SceneType.ActiveScene));

            if (activeScene.IsValid())
            {
                SceneManager.SetActiveScene(activeScene);
            }

            OnSceneGroupLoaded?.Invoke();
            _logger.Log($"All group is loaded...");
        }

        public async Task UnloadScenes()
        {
            _logger.Log("Start unloading scene group...");
            var sceneNamesToUnload = new List<string>();
            var activeScene = SceneManager.GetActiveScene().name;
            var sceneCount = SceneManager.sceneCount;

            
            for (var i = sceneCount - 1; i > 0; i--)
            {
                var sceneAt = SceneManager.GetSceneAt(i);
                if (!sceneAt.isLoaded) continue;
                
                var sceneName = sceneAt.name;
                
                if (sceneName.Equals(activeScene) 
                    || (sceneName == _bootstrapper && _isBootstrapPersistentScene)) continue;
                
                
#if SCENE_MANAGEMENT_ADDRESSABLES_ENABLED
                if(_handleGroup.Handles.Any(h=>h.IsValid() 
                                               && h.Result.Scene.name == sceneName)) continue;
#endif
                sceneNamesToUnload.Add(sceneName);
            }

            //Create an AsyncOperationGroup
            var operationGroup = new AsyncOperationGroup(sceneNamesToUnload.Count);

            foreach (var sceneNameToUnload in sceneNamesToUnload)
            {
                var operation = SceneManager.UnloadSceneAsync(sceneNameToUnload);
                if (operation == null) continue;

                operationGroup.Operations.Add(operation);

                OnSceneUnloaded?.Invoke(sceneNameToUnload);
            }
#if SCENE_MANAGEMENT_ADDRESSABLES_ENABLED
            foreach (var handle in _handleGroup.Handles.Where(handle => handle.IsValid()))
               await Addressables.UnloadSceneAsync(handle);
            
            _handleGroup.Handles.Clear();
#endif
            //Wait until all AsyncOperations in the group are done
            while (!operationGroup.IsDone)
            {
                await Task.Delay(100); // delay to avoid tight loop
            }

            if (_unloadRecoursesWithScenes)
            {
                await Resources.UnloadUnusedAssets();
            }
        }
    }
}