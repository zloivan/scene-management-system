using IKhom.SceneManagementSystem.Runtime.data;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(SceneLoader))]
public class SceneLoaderEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        SceneLoader sceneLoader = (SceneLoader) target;

        if (EditorApplication.isPlaying && GUILayout.Button("Load First Scene Group")) {
            LoadSceneGroup(sceneLoader, 0);
        }
            
        if (EditorApplication.isPlaying && GUILayout.Button("Load Second Scene Group")) {
            LoadSceneGroup(sceneLoader, 1);
        }
    }

    static async void LoadSceneGroup(SceneLoader sceneLoader, int index) {
        await sceneLoader.LoadSceneGroupAsync(index);
    }
}