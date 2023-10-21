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
using UnityEngine;

namespace SleepySceneManagement
{
    public static class CommonUtil
    {
        const string PREPEND = "<color=#72D248>SleepySceneManagement: </color>";

        public static void SleepySceneDebugLog(string str)
        {
            Debug.Log(PREPEND + str);
        }
        public static void SleepySceneDebugError(string str)
        {
            Debug.LogError(PREPEND + str);
        }
    }
}
#endif
