using UnityEngine;

namespace IKhom.SceneManagementSystem.Runtime.helpers
{
    internal static class GameobjectExtensions
    {
        public static T OrNull<T>(this T obj) where T : Object => obj ? obj : null;
    }
}