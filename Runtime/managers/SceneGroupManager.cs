using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IKhom.SceneManagementSystem.Runtime.data
{
    public class SceneGroupManager
    {
        public event Action<string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnloaded = delegate { };
        public event Action OnSceneGroupLoaded = delegate { };

        private readonly bool _isBootstrapPersistentScene;
        private readonly bool _unloadRecoursesWithScenes;

        public SceneGroup ActiveSceneGroup { get; private set; }

        public SceneGroupManager(bool isBootstrapPersistent, bool unloadRecoursesWithScenes)
        {
            _isBootstrapPersistentScene = isBootstrapPersistent;
            _unloadRecoursesWithScenes = unloadRecoursesWithScenes;
        }

        public async Task LoadScenes(SceneGroup group, IProgress<float> progress, bool reloadDupScenes = false)
        {
            ActiveSceneGroup = group;

            var loadedScenes = new List<string>();

            await UnloadScenes();

            var sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }

            var totalScenesToLoad = ActiveSceneGroup.Scenes.Count;

            var operationGroup = new AsyncOperationGroup(totalScenesToLoad);

            for (var i = 0; i < totalScenesToLoad; i++)
            {
                var sceneData = group.Scenes[i];

                if (reloadDupScenes == false && loadedScenes.Contains(sceneData.Name))
                {
                    continue;
                }
                
                var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
                
                await Task.Delay(TimeSpan.FromSeconds(2.5f)); //TODO REmove
                operationGroup.Operations.Add(operation);
                
                
                OnSceneLoaded?.Invoke(sceneData.Name);
            }

            //Wait until all AsyncOperations in the group are loaded
            while (!operationGroup.IsDone)
            {
                progress?.Report(operationGroup.Progress);
                await Task.Delay(100);
            }

            var activeScene =
                SceneManager.GetSceneByName(ActiveSceneGroup.FindSceneNameByType(SceneType.ActiveScene));

            if (activeScene.IsValid())
            {
                SceneManager.SetActiveScene(activeScene);
            }
            
            OnSceneGroupLoaded?.Invoke();
        }

        public async Task UnloadScenes()
        {
            var sceneNamesToUnload = new List<string>();
            var activeScene = SceneManager.GetActiveScene().name;

            var sceneCount = SceneManager.sceneCount;

            for (var i = sceneCount - 1; i > 0; i--)
            {
                var sceneAt = SceneManager.GetSceneAt(i);

                if (!sceneAt.isLoaded)
                {
                    continue;
                }

                var sceneName = sceneAt.name;

                if (sceneName.Equals(activeScene) || (sceneName == "Bootstrapper" && _isBootstrapPersistentScene))
                {
                    continue;
                }
                
                sceneNamesToUnload.Add(sceneName);
            }
            
            
            //Create an AsyncOperationGroup
            var operationGroup = new AsyncOperationGroup(sceneNamesToUnload.Count);

            foreach (var sceneNameToUnload in sceneNamesToUnload)
            {
                var operation = SceneManager.UnloadSceneAsync(sceneNameToUnload);
                if (operation == null)
                {
                    continue;
                }
                
                operationGroup.Operations.Add(operation);
                
                OnSceneUnloaded?.Invoke(sceneNameToUnload);
            }
            
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