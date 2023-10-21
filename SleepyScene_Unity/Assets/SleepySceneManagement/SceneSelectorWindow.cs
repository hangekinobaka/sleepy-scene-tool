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
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

namespace SleepySceneManagement
{
    public class SceneSelectorWindow : EditorWindow
    {
        /** For foldable scene list display **/
        private List<string> _sceneList = new List<string>();
        private List<string> _buildsceneList = new List<string>();
        private List<string> _fullSceneList = new List<string>();
        private Dictionary<string, List<string>> _sceneDict;
        private Dictionary<string, bool> _foldoutDict = new Dictionary<string, bool>();

        /** For select main entrance scene **/
        private string _entranceScenePath;

        /** Window UI only **/
        private Vector2 _scrollPosition;
        private bool _tipsShow = true;
        private bool _filterShow = true;

        /** Cache **/
        static SceneCache _sceneCache;

        /** For what scenes should be included **/
        private bool _includeBuildSettingScenesVal = true;
        private bool _includeBuildSettingScenes
        {
            get { return _includeBuildSettingScenesVal; }
            set
            {
                if (_includeBuildSettingScenesVal != value)
                {
                    _includeBuildSettingScenesVal = value;
                    _onIncludeScenesChanged?.Invoke();
                }
            }
        }
        private bool _includeOtherScenesVal = false;
        private bool _includeOtherScenes
        {
            get { return _includeOtherScenesVal; }
            set
            {
                if (_includeOtherScenesVal != value)
                {
                    _includeOtherScenesVal = value;
                    _onIncludeScenesChanged?.Invoke();
                }
            }
        }
        private UnityAction _onIncludeScenesChanged;

        [MenuItem("Sleepy Scene/Open Select Scene Window _WINDOW")]
        public static void ShowWindow()
        {
            GetWindow<SceneSelectorWindow>("Select Scene");
        }

        private void OnEnable()
        {
            if (_sceneCache == null) _sceneCache = SceneCache.GetSceneCache();

            // Load the saved entrance scene path
            _entranceScenePath = _sceneCache.EntranceScenePath;

            UpdateSceneList();
            _onIncludeScenesChanged += UpdateSceneList;
        }

        private void OnDisable()
        {
            _onIncludeScenesChanged -= UpdateSceneList;
        }

        private void UpdateSceneList()
        {
            if (_includeBuildSettingScenes && _includeOtherScenes)
            {
                _sceneList = GetAllScenePaths();
            }
            else if (_includeBuildSettingScenes && !_includeOtherScenes)
            {
                _sceneList = GetAllBuildScenePaths();
            }
            else if (!_includeBuildSettingScenes && _includeOtherScenes)
            {
                GetAllBuildScenePaths();
                GetAllScenePaths();
                _sceneList = _fullSceneList.Except(_buildsceneList).ToList();
            }
            else
            {
                _sceneList.Clear();
            }
            // Organize scenes by path
            _sceneDict = new Dictionary<string, List<string>>();
            foreach (string path in _sceneList)
            {
                string folderPath = Path.GetDirectoryName(path);
                if (!_sceneDict.ContainsKey(folderPath))
                {
                    _sceneDict[folderPath] = new List<string>();
                }
                _sceneDict[folderPath].Add(path);
            }
        }

        private List<string> GetAllBuildScenePaths()
        {
            _buildsceneList.Clear();
            // Get all scenes from the build settings
            var scenes = EditorBuildSettings.scenes;

            foreach (var scene in scenes)
            {
                // We make sure all the path use "/" as the separator (Especially for Windows)
                _buildsceneList.Add(scene.path.Replace("\\", "/"));
            }

            return _buildsceneList;
        }

        private List<string> GetAllScenePaths()
        {
            _fullSceneList.Clear();
            string[] allFiles = Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories);

            foreach (var file in allFiles)
            {
                // We make sure all the path use "/" as the separator (Especially for Windows)
                string relativePath = "Assets" + file.Substring(Application.dataPath.Length).Replace("\\", "/");
                _fullSceneList.Add(relativePath);
            }

            return _fullSceneList;
        }

        #region GUI
        private void OnGUI()
        {
            /** How to use tips **/
            _tipsShow = EditorGUILayout.Foldout(_tipsShow, "How to use");

            if (_tipsShow)
            {
                GUILayout.BeginVertical("box");

                GUIStyle wordWrapStyle = new GUIStyle(EditorStyles.label);
                wordWrapStyle.wordWrap = true;

                EditorGUILayout.LabelField("\u2610: Tick to mark the entry scene.", wordWrapStyle);
                EditorGUILayout.LabelField("O: Open Scene", wordWrapStyle);
                EditorGUILayout.LabelField("+: Open Scene Additively", wordWrapStyle);

                EditorGUILayout.Space();
                GUILayout.EndVertical();
            }
            EditorGUILayout.Space();

            /** Scene Selection Filter **/
            _filterShow = EditorGUILayout.Foldout(_filterShow, "Scene Filter");

            if (_filterShow)
            {
                GUILayout.BeginVertical("box");

                GUILayout.BeginHorizontal();
                _includeBuildSettingScenes = GUILayout.Toggle(_includeBuildSettingScenes, "Build Setting");
                _includeOtherScenes = GUILayout.Toggle(_includeOtherScenes, "Others");
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("All", GUILayout.Width(50), GUILayout.Height(20)))
                {
                    _includeBuildSettingScenes = true;
                    _includeOtherScenes = true;
                }

                if (GUILayout.Button("None", GUILayout.Width(50), GUILayout.Height(20)))
                {
                    _includeBuildSettingScenes = false;
                    _includeOtherScenes = false;
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();
                GUILayout.EndVertical();
            }
            EditorGUILayout.Space();

            /** Scene list **/
            EditorGUILayout.LabelField("Scene List:", EditorStyles.boldLabel);  // Bold title
            // Create a style for red text
            GUIStyle redTextStyle = new GUIStyle(GUI.skin.label);
            redTextStyle.normal.textColor = Color.red;

            // Create a custom style for text with overflow clipping 
            GUIStyle textStyle = new GUIStyle(GUI.skin.label);

            // Create a scroll view for the scene list
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var folderPath in _sceneDict.Keys)
            {
                if (!_foldoutDict.ContainsKey(folderPath))
                {
                    _foldoutDict[folderPath] = true;
                }

                _foldoutDict[folderPath] = EditorGUILayout.Foldout(_foldoutDict[folderPath], folderPath);

                if (_foldoutDict[folderPath])
                {
                    foreach (var scenePath in _sceneDict[folderPath])
                    {
                        EditorGUILayout.BeginHorizontal();

                        // Is Entrance checkbox
                        bool isEntrance = _entranceScenePath == scenePath;
                        bool newIsEntrance = GUILayout.Toggle(isEntrance, "");
                        if (newIsEntrance != isEntrance)
                        {
                            _entranceScenePath = newIsEntrance ? scenePath : "";
                            if (_sceneCache == null) _sceneCache = SceneCache.GetSceneCache();
                            _sceneCache.EntranceScenePath = _entranceScenePath;
                            EditorUtility.SetDirty(_sceneCache);  // Mark the object as dirty
                            AssetDatabase.SaveAssets();  // Save all modified assets
                        }

                        // Scene name
                        string fileName = System.IO.Path.GetFileName(scenePath);
                        GUILayout.Label(fileName, textStyle, GUILayout.ExpandWidth(true));
                        if (isEntrance)
                        {
                            GUILayout.Label("entrance", redTextStyle);
                        }

                        GUILayout.FlexibleSpace();  // Pushes the following buttons to the right

                        // Buttons for opening the scene
                        if (GUILayout.Button("O", GUILayout.Width(30), GUILayout.Height(20)))
                        {
                            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                        }
                        if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(20)))
                        {
                            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.EndScrollView();

        }
        #endregion
    }
}
#endif
