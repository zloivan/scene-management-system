using IKhom.SceneManagementSystem.Runtime.data;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(SceneLoader))]
public class SceneLoaderEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        var sceneLoader = (SceneLoader) target;

        if (!EditorApplication.isPlaying) return;
        
        var numberOfGroups = sceneLoader.SceneGroups;

        for (var i = 0; i < numberOfGroups.Length; i++)
        {
            if (GUILayout.Button($"Load Scene Group: {numberOfGroups[i].GroupName}")) {
                LoadSceneGroup(sceneLoader, i);
            }
        }
    }

    static async void LoadSceneGroup(SceneLoader sceneLoader, int index) {
        await sceneLoader.LoadSceneGroupAsync(index);
    }
}