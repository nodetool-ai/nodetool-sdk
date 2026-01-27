using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Nodetool.SDK.Unity
{
    public static class UnityAudioLoader
    {
        public static async Task<AudioClip> LoadAudioClipAsync(string localPath, AudioType audioType = AudioType.UNKNOWN)
        {
            var uri = new System.Uri(localPath).AbsoluteUri;
            using var request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new UnityException(request.error);
            }

            return DownloadHandlerAudioClip.GetContent(request);
        }
    }
}
