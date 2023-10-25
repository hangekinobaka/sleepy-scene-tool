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
using UnityEngine.SceneManagement;

namespace SleepySceneManagement
{
    public class SceneSelectorWindow : EditorWindow
    {
        const int CANCEL_SCENE_HOVER_COUNTER = 3;

        /** For foldable scene list display **/
        private List<string> _sceneList = new List<string>();
        private List<string> _buildsceneList = new List<string>();
        private List<string> _fullSceneList = new List<string>();
        private Dictionary<string, List<string>> _sceneDict;
        private Dictionary<string, bool> _foldoutDict = new Dictionary<string, bool>();

        /** For select main entrance scene **/
        private string _entranceScenePath;

        /** For display which scene is under editing **/
        private HashSet<string> editingSceneList = new HashSet<string>();

        /** For GUI only **/
        private Vector2 _scrollPosition;
        private bool _tipsShow = true;
        private bool _filterShow = true;
        private string _hoveredScenePath;
        private int _cancelSceneHoverCounter = CANCEL_SCENE_HOVER_COUNTER;

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
            UpdatEditingSceneList();

            _onIncludeScenesChanged += UpdateSceneList;
            EditorSceneManager.sceneOpened += OnSceneLoaded;
        }

        private void OnDisable()
        {
            _onIncludeScenesChanged -= UpdateSceneList;
            EditorSceneManager.sceneOpened -= OnSceneLoaded;
        }

        #region Get editing scene list methods
        public void OnSceneLoaded(Scene scene, OpenSceneMode mode)
        {
            // The editing scene are changed
            UpdatEditingSceneList();
        }

        private void UpdatEditingSceneList()
        {
            editingSceneList.Clear();
            int countLoaded = SceneManager.sceneCount;

            for (int i = 0; i < countLoaded; i++)
            {
                string path = SceneManager.GetSceneAt(i).path.Replace("\\", "/");
                editingSceneList.Add(path);
            }
        }
        #endregion

        #region Get all scene list methods
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
                // We make sure all the path use "/" as the separator (Especially for Windows)
                string folderPath = Path.GetDirectoryName(path).Replace("\\", "/");
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
        #endregion

        private void OnGUI()
        {
            #region How to use tips
            _tipsShow = EditorGUILayout.Foldout(_tipsShow, "How to use");

            if (_tipsShow)
            {
                GUILayout.BeginVertical("box");

                GUIStyle wordWrapStyle = new GUIStyle(EditorStyles.boldLabel);
                wordWrapStyle.wordWrap = true;

                EditorGUILayout.LabelField("Click the scene name to mark it as the entrance.", wordWrapStyle);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("O: Open Scene", wordWrapStyle);
                EditorGUILayout.LabelField("+: Open Scene Additively", wordWrapStyle);
                EditorGUILayout.LabelField("Note: \u2192 Tells you which scene is under editing", wordWrapStyle);

                GUILayout.EndVertical();
            }
            EditorGUILayout.Space();
            #endregion

            #region Scene Selection Filter
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
            #endregion

            #region Scene list
            // Title
            EditorGUILayout.LabelField("Scene List:", EditorStyles.boldLabel);  // Bold title

            // Create a scroll view for the scene list
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Create scene name GUIStyle
            GUIStyle sceneNameStyle = new GUIStyle(EditorStyles.toolbarButton);
            sceneNameStyle.alignment = TextAnchor.MiddleLeft;  // Set text alignment to left
            sceneNameStyle.richText = true;

            foreach (var folderPath in _sceneDict.Keys)
            {
                if (!_foldoutDict.ContainsKey(folderPath))
                {
                    _foldoutDict[folderPath] = true;
                }

                _foldoutDict[folderPath] = EditorGUILayout.Foldout(_foldoutDict[folderPath], folderPath);

                if (_foldoutDict[folderPath])
                {
                    EditorGUILayout.BeginVertical("box");

                    Color originalColor = GUI.backgroundColor;
                    foreach (var scenePath in _sceneDict[folderPath])
                    {
                        Rect sceneRect = EditorGUILayout.BeginHorizontal();
                        // Highlight row selection 
                        if (_hoveredScenePath == scenePath)
                        {
                            GUI.backgroundColor = Color.cyan;  // Change background color for selected scene

                            if (!sceneRect.Contains(Event.current.mousePosition))
                            {
                                // Hack: it seems that unity's gui mouse event detection is very unstable. So I give it a buffer. 
                                _cancelSceneHoverCounter--;
                                if (_cancelSceneHoverCounter <= 0)
                                {
                                    _cancelSceneHoverCounter = CANCEL_SCENE_HOVER_COUNTER;

                                    _hoveredScenePath = "";  // Update selected scene when line is being hovered
                                    Repaint();  // Request the window to be repainted
                                }
                            }
                        }
                        else if (sceneRect.Contains(Event.current.mousePosition))
                        {
                            _hoveredScenePath = scenePath;  // Update selected scene when line is being hovered
                            Repaint();  // Request the window to be repainted
                        }

                        // Scene name and Entrance Selection and Editing Scene display
                        bool isEntrance = _entranceScenePath == scenePath;
                        string fileName = Path.GetFileName(scenePath);
                        if (GUILayout.Button(
                            (editingSceneList.Contains(scenePath) ? "\u2192 " : "\u00A0\u00A0\u00A0\u00A0") +
                            fileName +
                            (isEntrance ? "    <color=red>entrance</color>" : ""),
                            sceneNameStyle, GUILayout.ExpandWidth(true)))
                        {
                            _entranceScenePath = scenePath;

                            // Save the selection to cache
                            if (_sceneCache == null) _sceneCache = SceneCache.GetSceneCache();
                            _sceneCache.EntranceScenePath = _entranceScenePath;
                            EditorUtility.SetDirty(_sceneCache);  // Mark the object as dirty
                            AssetDatabase.SaveAssets();  // Save all modified assets
                        }

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

                        // Highlight row selection ends here
                        GUI.backgroundColor = originalColor;  // Restore original background color
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndScrollView();
            #endregion
        }
    }
}
#endif
