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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SleepySceneManagement
{
    public class SceneSelectorWindow : EditorWindow
    {
        /** For foldable scene list display **/
        private Dictionary<string, List<string>> _sceneDict;
        private Dictionary<string, bool> _foldoutDict = new Dictionary<string, bool>();

        /** For select main entrance scene **/
        private string _entranceScenePath;

        /** Window UI only **/
        private Vector2 _scrollPosition;

        /** Cache **/
        static SceneCache _sceneCache;

        [MenuItem("Sleepy Scene/Open Select Scene Window _WINDOW")]
        public static void ShowWindow()
        {
            GetWindow<SceneSelectorWindow>("Select Scene");
        }

        private void OnEnable()
        {
            if (_sceneCache == null) _sceneCache = SceneCache.GetSceneCache();

            // Get all scenes from the build settings
            var scenes = EditorBuildSettings.scenes;
            _sceneDict = new Dictionary<string, List<string>>();

            // Organize scenes by full path
            foreach (var scene in scenes)
            {
                // We make sure all the path use "/" as the separator (Especially for Windows)
                string fullPath = System.IO.Path.GetDirectoryName(scene.path).Replace("\\", "/");
                if (!_sceneDict.ContainsKey(fullPath))
                {
                    _sceneDict[fullPath] = new List<string>();
                }
                _sceneDict[fullPath].Add(scene.path.Replace("\\", "/"));
            }

            // Load the saved entrance scene path
            _entranceScenePath = _sceneCache.EntranceScenePath;
        }

        private void OnGUI()
        {
            /** How to use tips **/
            GUILayout.BeginVertical("box");

            GUIStyle wordWrapStyle = new GUIStyle(EditorStyles.label);
            wordWrapStyle.wordWrap = true;

            EditorGUILayout.LabelField("How to Use:", EditorStyles.boldLabel);  // Bold title
                                                                                // Explanation of symbols
            EditorGUILayout.LabelField("Checkbox: Tick to mark the entry scene.", wordWrapStyle);
            EditorGUILayout.LabelField("O: Open Scene", wordWrapStyle);
            EditorGUILayout.LabelField("+: Open Scene Additively", wordWrapStyle);

            GUILayout.EndVertical();

            EditorGUILayout.Space();  // Add some space between tips and scenes list

            /** Scene list **/
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
    }
}
#endif
