#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sleepy.SceneTool
{
    public class ScenePlayUtil : ScriptableObject
    {// The clase extends ScriptableObject for getting current file's path.

        static SceneCache _sceneCache;

        [MenuItem("Sleepy/SceneTool/\u25B6 Play From Entrance Scene")]
        public static void PlayFromEntrance()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {

                if (_sceneCache == null) _sceneCache = SceneCache.GetSceneCache();
    
                // Get entranceScenePath from the scene cache
                string entranceScenePath = _sceneCache.EntranceScenePath;
    
                if (string.IsNullOrEmpty(entranceScenePath) || !System.IO.File.Exists(entranceScenePath))
                {
                    CommonUtil.SleepySceneDebugError($"Your entrance scene {entranceScenePath} does not exist!\nPlease go to the Select Scene Window and set a valid entrance.");
                    return;
                }
    
                _sceneCache.EditingScenesPathsCache.Clear();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    _sceneCache.EditingScenesPathsCache.Add(scene.path);
                }
    
                try
                {
                    EditorSceneManager.OpenScene(entranceScenePath);
                }
                catch (Exception e)
                {
                    CommonUtil.SleepySceneDebugError("Open scene faild! " + e);
                    return;
                }
    
                EditorApplication.isPlaying = true;
            }
        }

        [MenuItem("Sleepy/SceneTool/\u23F9 Stop And Resume Editing Scene(s)")]
        public static void StopAndResume()
        {
            EditorApplication.isPlaying = false;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (_sceneCache == null) _sceneCache = SceneCache.GetSceneCache();
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (_sceneCache.EditingScenesPathsCache.Count == 0) return;

                string firstScenePath = _sceneCache.EditingScenesPathsCache[0];
                if (string.IsNullOrEmpty(firstScenePath) || !System.IO.File.Exists(firstScenePath))
                {
                    CommonUtil.SleepySceneDebugError("Invalid scene path: " + firstScenePath);
                    return;
                }

                EditorSceneManager.OpenScene(firstScenePath);

                for (int i = 1; i < _sceneCache.EditingScenesPathsCache.Count; i++)
                {
                    string scenePath = _sceneCache.EditingScenesPathsCache[i];
                    if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
                    {
                        CommonUtil.SleepySceneDebugError("Invalid scene path: " + scenePath);
                        continue;
                    }

                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }

                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }
            _sceneCache.EditingScenesPathsCache.Clear();
        }
    }
}
#endif
