#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nodetool.SDK.Unity
{
    public static class NodetoolSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Nodetool", SettingsScope.Project)
            {
                label = "Nodetool",
                guiHandler = _ => DrawSettings(),
                keywords = new HashSet<string>(new[] { "Nodetool", "SDK", "Workflow", "API" })
            };
        }

        private static void DrawSettings()
        {
            var settings = NodetoolSettings.Load();
            EditorGUI.BeginChangeCheck();

            settings.workerWebSocketUrl = EditorGUILayout.TextField("Worker WebSocket Url", settings.workerWebSocketUrl);
            settings.apiBaseUrl = EditorGUILayout.TextField("API Base Url", settings.apiBaseUrl);
            settings.apiKey = EditorGUILayout.TextField("API Key", settings.apiKey);
            settings.userId = EditorGUILayout.TextField("User Id", settings.userId);
            settings.cacheDirectory = EditorGUILayout.TextField("Cache Directory", settings.cacheDirectory);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
#endif
