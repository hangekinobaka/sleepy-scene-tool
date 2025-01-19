#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Sleepy.SceneTool
{
    public class SceneSelectorWindow : EditorWindow
    {
        const int CANCEL_SCENE_HOVER_COUNTER = 3;

        /** Textures **/
        private Texture2D _playIcon;
        private Texture2D _stopIcon;

        /** For foldable scene list display **/
        private List<string> _sceneList = new List<string>();
        private List<string> _buildsceneList = new List<string>();
        private List<string> _fullSceneList = new List<string>();
        private Dictionary<string, List<string>> _sceneDict;
        private Dictionary<string, bool> _foldoutDict = new Dictionary<string, bool>();

        /** For select main entrance scene **/
        private string _entranceScenePath;

        /** For display which scene is under editing **/
        private HashSet<string> _editingSceneList = new HashSet<string>();

        /** For GUI only **/
        private Vector2 _scrollPosition;
        private bool _tipsShow = false;
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

        [MenuItem("Sleepy/SceneTool/Open Select Scene Window _WINDOW")]
        public static void ShowWindow()
        {
            GetWindow<SceneSelectorWindow>("Select Scene");
        }

        private void Awake()
        {
            _playIcon = CommonUtil.LoadPlayIcon();
            _stopIcon = CommonUtil.LoadStopIcon();
        }

        private void OnEnable()
        {
            if (_sceneCache == null) _sceneCache = SceneCache.GetSceneCache();

            // Load the saved entrance scene path
            _entranceScenePath = _sceneCache.EntranceScenePath;

            UpdateSceneList();
            UpdatEditingSceneList();

            _onIncludeScenesChanged += UpdateSceneList;
            EditorSceneManager.sceneOpened += OnSceneAdded;
            EditorSceneManager.sceneClosed += OnSceneRemoved;
        }

        private void OnDisable()
        {
            _onIncludeScenesChanged -= UpdateSceneList;
            EditorSceneManager.sceneOpened -= OnSceneAdded;
            EditorSceneManager.sceneClosed -= OnSceneRemoved;
        }

        #region Get editing scene list methods  
        public async void OnSceneRemoved(Scene scene)
        {
            // Apparently when this handler is called, the scene is not actually removed.
            // So we wait for a while to get the new list.
            await Task.Delay(10);
            // The editing scene are changed
            UpdatEditingSceneList();
        }

        public void OnSceneAdded(Scene scene, OpenSceneMode mode)
        {
            // The editing scene are changed
            UpdatEditingSceneList();
        }

        private void UpdatEditingSceneList()
        {
            _editingSceneList.Clear();
            int countLoaded = SceneManager.sceneCount;

            for (int i = 0; i < countLoaded; i++)
            {
                string path = SceneManager.GetSceneAt(i).path.Replace("\\", "/");
                _editingSceneList.Add(path);
            }

            Repaint();  // Request the window to be repainted
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
                // We need to check if there is any invalid scene in the build setting
                if (string.IsNullOrEmpty(path))
                {
                    CommonUtil.SleepySceneDebugError("There is invalid scene in your build setting");
                    continue;
                }

                // We make sure all the path use "/" as the separator (Especially for Windows)
                string folderPath = Path.GetDirectoryName(path).Replace("\\", "/");
                if (!_sceneDict.ContainsKey(folderPath))
                {
                    _sceneDict[folderPath] = new List<string>();
                }
                _sceneDict[folderPath].Add(path);
            }

            Repaint();  // Request the window to be repainted
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
            // Default vars
            Color originalColor = GUI.backgroundColor;

            #region Control panel
            GUILayout.BeginVertical("Box");

            // Title
            EditorGUILayout.LabelField("Control panel:", EditorStyles.boldLabel);  // Bold title
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();  // Add flexible space on the left

            // Show play button only when the app is not playing, otherwise show stop btton only.
            if (EditorApplication.isPlaying)
            {
                if (GUILayout.Button(new GUIContent(_stopIcon, "Stop And Resume Editing Scene(s)"), GUILayout.Width(50), GUILayout.Height(30)))
                {
                    ScenePlayUtil.StopAndResume();
                }
            }
            else
            {
                if (GUILayout.Button(new GUIContent(_playIcon, "Play From Entrance Scene"), GUILayout.Width(50), GUILayout.Height(30)))
                {
                    ScenePlayUtil.PlayFromEntrance();
                }
            }

            GUILayout.FlexibleSpace();  // Add flexible space on the right
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUILayout.EndVertical();
            #endregion

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
                EditorGUILayout.LabelField("-: Remove Scene", wordWrapStyle);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("\u2192 at the beginning of each row tells you which scene is under editing", wordWrapStyle);

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
            GUILayout.BeginHorizontal();
            // Title
            GUILayout.Label("Scene List:", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));  // Bold title

            // Refresh button
            GUILayout.Space(10f);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(new GUIContent("\u21BB", "Refresh the scene list"), GUILayout.Width(22), GUILayout.Height(20)))
            {
                UpdateSceneList();
            }
            GUI.backgroundColor = originalColor;  // Restore the original color

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

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
                            (_editingSceneList.Contains(scenePath) ? "\u2192 " : "\u00A0\u00A0\u00A0\u00A0") +
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
                        if (GUILayout.Button("O", GUILayout.Width(22), GUILayout.Height(20)))
                        {
                            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                        }
                        if (GUILayout.Button("+", GUILayout.Width(22), GUILayout.Height(20)))
                        {
                            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                        }
                        GUI.enabled = _editingSceneList.Count > 1 && _editingSceneList.Contains(scenePath);
                        // - will only be enabled when
                        // 1. it is being edting
                        // 2. thi is not the last scene editing
                        if (GUILayout.Button("-", GUILayout.Width(22), GUILayout.Height(20)))
                        {
                            var scene = EditorSceneManager.GetSceneByPath(scenePath);
                            // check if dirty
                            if (scene.isDirty)
                            {
                                int option = EditorUtility.DisplayDialogComplex(
                                    "Scene Has Been Modified",
                                    "Do you want to save the changes you made in the scene: " + scene.name,
                                    "Save", // Option 0
                                    "Don't Save",  // Option 1
                                    "Cancel" // Option 2
                                );

                                switch (option)
                                {
                                    case 0: // Save
                                        EditorSceneManager.SaveScene(scene);
                                        EditorSceneManager.CloseScene(scene, true);
                                        break;
                                    case 1: // Don't Save
                                        EditorSceneManager.CloseScene(scene, true);
                                        break;
                                    case 2: // Cancel
                                            // Do nothing, cancel the operation
                                        break;
                                }
                            }
                            else
                            {
                                EditorSceneManager.CloseScene(scene, true);
                            }
                        }
                        GUI.enabled = true;

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
