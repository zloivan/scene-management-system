using System;
using System.Collections.Generic;
using System.Linq;
using Eflatun.SceneReference;

namespace IKhom.SceneManagementSystem.Runtime.data
{
    [Serializable]
    public class SceneData
    {
        public SceneReference Reference;
        public string Name => Reference.Name;
        public SceneType SceneType;
    }

    [Serializable]
    public class SceneGroup
    {
        public string GroupName = "New Scene Group";
        public List<SceneData> Scenes;

        public string FindSceneNameByType(SceneType type)
        {
            return Scenes.FirstOrDefault(s => s.SceneType == type)?.Reference.Name;
        }
    }
}