#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sleepy.SceneTool
{
    public static class CommonUtil
    {
        const string PREPEND = "<color=#72D248>SleepySceneTool: </color>";
        const string PLAY_ICON_PATH = "Icons/play_icon.png";
        const string STOP_ICON_PATH = "Icons/stop_icon.png";

        public static void SleepySceneDebugLog(string str)
        {
            Debug.Log(PREPEND + str);
        }
        public static void SleepySceneDebugError(string str)
        {
            Debug.LogError(PREPEND + str);
        }

        public static string GetDirectoryPath()
        {
            return SceneCache.GetDirectoryPath();
        }

        public static Texture2D LoadImage(string relativePath)
        {
            string absolutePath = System.IO.Path.Combine(GetDirectoryPath(), relativePath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(absolutePath);
        }
        public static Texture2D LoadPlayIcon()
        {
            string absolutePath = System.IO.Path.Combine(GetDirectoryPath(), PLAY_ICON_PATH);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(absolutePath);
        }
        public static Texture2D LoadStopIcon()
        {
            string absolutePath = System.IO.Path.Combine(GetDirectoryPath(), STOP_ICON_PATH);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(absolutePath);
        }
    }
}
#endif
