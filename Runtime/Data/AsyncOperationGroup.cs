using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if SCENE_MANAGEMENT_ADDRESSABLES_ENABLED
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

namespace IKhom.SceneManagementSystem.Runtime.data
{
    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> Operations;
        public float Progress => Operations.Count == 0 ? 0 : Operations.Average(o => o.progress);
        public bool IsDone => Operations.All(o => o.isDone);

        public AsyncOperationGroup(int initialCapacity)
        {
            Operations = new List<AsyncOperation>(initialCapacity);
        }
    }

    public readonly struct AsyncOperationHandleGroup
    {
#if SCENE_MANAGEMENT_ADDRESSABLES_ENABLED
        public readonly List<AsyncOperationHandle<SceneInstance>> Handles;
        
        public float Progress => Handles.Count == 0 
            ? 0 
            : Handles.Average(h => h.PercentComplete);

        public bool IsDone => Handles.All(h => h.IsDone);

        public AsyncOperationHandleGroup(int initialCapacity)
        {
            Handles = new List<AsyncOperationHandle<SceneInstance>>(initialCapacity);
        }
#endif
    }

}