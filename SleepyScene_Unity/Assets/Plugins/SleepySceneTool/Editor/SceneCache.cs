#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sleepy.SceneTool
{
    public class SceneCache : ScriptableObject
    {
        const string SCENE_CACHE_ASSET_DIR = "Plugins/SleepySceneTool/Editor";
        const string SCENE_CACHE_ASSET_PATH = "Assets/" + SCENE_CACHE_ASSET_DIR + "/SceneCache.asset";
        const string DEFAULT_ENTRANCE_SCENE = "Assets/Scenes/Main.unity"; // Default entrance scene

        public List<string> EditingScenesPathsCache = new List<string>();
        public string EntranceScenePath = DEFAULT_ENTRANCE_SCENE;

        static SceneCache _sceneCache;

        [InitializeOnLoadMethod]
        private static void Initialize() // Initialize will be excuted when this script is being compiled
        {
            GetSceneCache();
        }

        public static string GetDirectoryPath()
        {
            // Get our current path
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(ScriptableObject.CreateInstance<SceneCache>()));
            return Path.GetDirectoryName(scriptPath);
        }

        public static SceneCache GetSceneCache()
        {
            if (_sceneCache != null) return _sceneCache;

            string assetPath = SCENE_CACHE_ASSET_PATH;

            // Load the ScriptableObject, or create a new one if it doesn't exist
            _sceneCache = AssetDatabase.LoadAssetAtPath<SceneCache>(assetPath);
            if (_sceneCache == null)
            {
                string dirPath = Path.Combine(Application.dataPath, SCENE_CACHE_ASSET_DIR);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                _sceneCache = ScriptableObject.CreateInstance<SceneCache>();
                AssetDatabase.CreateAsset(_sceneCache, assetPath);
                CommonUtil.SleepySceneDebugLog($"A SceneCache Object is created in path: {assetPath}");
            }
            return _sceneCache;
        }
    }
}
#endif
