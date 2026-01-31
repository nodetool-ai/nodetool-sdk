using UnityEngine;

namespace Nodetool.SDK.Unity
{
    [CreateAssetMenu(fileName = "NodetoolSettings", menuName = "Nodetool/Settings")]
    public class NodetoolSettings : ScriptableObject
    {
        [Header("Connection")]
        public string workerWebSocketUrl = "ws://localhost:7777/ws";
        public string apiBaseUrl = "http://localhost:7777";

        [Header("Authentication")]
        public string apiKey = "";
        public string userId = "";

        [Header("Cache")]
        public string cacheDirectory = "";

        public static NodetoolSettings Load()
        {
            return Resources.Load<NodetoolSettings>("NodetoolSettings") ?? CreateInstance<NodetoolSettings>();
        }
    }
}
