#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Stranogene.Games.Oltre.Editor
{
    /// <summary>
    /// AutoBootPlayMode
    /// Se premi Play mentre non sei nella scena 0,
    /// salva la scena corrente e avvia la scena 0.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoBootPlayMode
    {
        static AutoBootPlayMode()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            var bootScenePath = SceneUtility.GetScenePathByBuildIndex(0);
            var activeScene = SceneManager.GetActiveScene();

            if (activeScene.path != bootScenePath)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(bootScenePath);
                }
            }
        }
    }
}
#endif