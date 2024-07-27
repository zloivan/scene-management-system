using System;

namespace IKhom.SceneManagementSystem.Runtime.data
{
    public class SceneLoadProgress : IProgress<float>
    {
        public event Action<float> Progressed;

        private const float RATIO = 1f;

        public void Report(float value)
        {
            Progressed?.Invoke(value / RATIO);
        }
    }
}