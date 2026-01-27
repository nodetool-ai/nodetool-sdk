using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Nodetool.SDK.Unity
{
    public static class UnityAudioLoader
    {
        public static Task<AudioClip> LoadAudioClipAsync(string localPath, AudioType audioType = AudioType.UNKNOWN)
        {
            var uri = new System.Uri(localPath).AbsoluteUri;
            var request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            var tcs = new TaskCompletionSource<AudioClip>();

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        tcs.TrySetException(new UnityException(request.error));
                    }
                    else
                    {
                        tcs.TrySetResult(DownloadHandlerAudioClip.GetContent(request));
                    }
                }
                finally
                {
                    request.Dispose();
                }
            };

            return tcs.Task;
        }
    }
}
