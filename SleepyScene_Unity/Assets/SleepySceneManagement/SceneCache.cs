/*
 * Sleepy Scene Management
 * Copyright (c) 2023 He Yiran
 * 
 * This file is part of the Sleepy Scene Management software, 
 * which is licensed under the terms and conditions of the custom license 
 * provided with the software package.
 * 
 * You may use, modify, and keep this software as long as you comply 
 * with the license terms. The full license can be found in the LICENSE file 
 * included with this software or can be obtained by contacting the author.
 * 
 * Any modification to the software is done at your own risk. The author 
 * is not responsible for any issues arising from modifications to the software.
 * 
 * For any issues with the unmodified software, please contact the author.
 */

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SleepySceneManagement
{
    public class SceneCache : ScriptableObject
    {
        const string SCENE_CACHE_ASSET_PATH = "SceneCache.asset";
        const string DEFAULT_ENTRANCE_SCENE = "Assets/Scenes/Main.unity"; // Default entrance scene

        public List<string> EditingScenesPathsCache = new List<string>();
        public string EntranceScenePath = DEFAULT_ENTRANCE_SCENE;

        static SceneCache _sceneCache;

        [InitializeOnLoadMethod]
        private static void Initialize() // Initialize will be excuted when this script is being compiled
        {
            GetSceneCache();
        }

        public static SceneCache GetSceneCache()
        {
            if (_sceneCache != null) return _sceneCache;

            // Get the path of ScenePlayUtil script
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(ScriptableObject.CreateInstance<SceneCache>()));
            string directoryPath = Path.GetDirectoryName(scriptPath);

            // Construct the path for SceneCache asset
            string assetPath = Path.Combine(directoryPath, SCENE_CACHE_ASSET_PATH);

            // Load the ScriptableObject, or create a new one if it doesn't exist
            _sceneCache = AssetDatabase.LoadAssetAtPath<SceneCache>(assetPath);
            if (_sceneCache == null)
            {
                _sceneCache = ScriptableObject.CreateInstance<SceneCache>();
                AssetDatabase.CreateAsset(_sceneCache, assetPath);
                CommonUtil.SleepySceneDebugLog($"A SceneCache Object is created in path: {assetPath}");
            }
            return _sceneCache;
        }
    }
}
#endif
