using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Nodetool.SDK.Assets;
using Nodetool.SDK.Values;
using UnityEngine;

namespace Nodetool.SDK.Unity
{
    public static class UnityAssetManager
    {
        private static AssetManager _assetManager;

        public static AssetManager AssetManager
        {
            get
            {
                if (_assetManager == null)
                {
                    var settings = NodetoolSettings.Load();
                    var cacheRoot = string.IsNullOrWhiteSpace(settings.cacheDirectory)
                        ? Application.temporaryCachePath
                        : settings.cacheDirectory;
                    var cachePath = Path.Combine(cacheRoot, "nodetool_cache");
                    _assetManager = new AssetManager(cachePath);
                }

                return _assetManager;
            }
        }

        public static async Task<Texture2D> LoadTextureAsync(NodeToolValue value, Uri apiBaseUrl)
        {
            if (value.Kind != NodeToolValueKind.Map)
            {
                throw new ArgumentException("Expected image map value (e.g. {type:\"image\", uri:\"...\"}).");
            }

            var map = value.AsMapOrEmpty();
            if (!map.TryGetValue("uri", out var uriValue))
            {
                if (map.TryGetValue("value", out var inner) && inner.Kind == NodeToolValueKind.Map)
                {
                    var innerMap = inner.AsMapOrEmpty();
                    if (!innerMap.TryGetValue("uri", out uriValue))
                    {
                        throw new ArgumentException("Image has no 'uri' field (neither root nor value.uri).");
                    }
                }
                else
                {
                    throw new ArgumentException("Image has no 'uri' field.");
                }
            }

            var uri = uriValue.AsString();
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentException("Image URI is empty.");
            }

            uri = NormalizeAssetUri(uri, apiBaseUrl);

            var localPath = await AssetManager.DownloadAssetAsync(uri);
            return LoadTextureFromFile(localPath);
        }

        private static string NormalizeAssetUri(string uri, Uri apiBaseUrl)
        {
            if (uri.StartsWith("/", StringComparison.Ordinal))
            {
                return new Uri(apiBaseUrl, uri).ToString();
            }

            return uri;
        }

        public static Texture2D LoadTextureFromFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);
            return texture;
        }

        public static string TextureToDataUri(Texture2D texture)
        {
            var bytes = texture.EncodeToPNG();
            var base64 = Convert.ToBase64String(bytes);
            return $"data:image/png;base64,{base64}";
        }

        public static Dictionary<string, object> TextureToImageRef(Texture2D texture)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "image",
                ["uri"] = TextureToDataUri(texture)
            };
        }

        public static async Task<string> DownloadAudioAsync(NodeToolValue value, Uri apiBaseUrl)
        {
            if (value.Kind != NodeToolValueKind.Map)
            {
                throw new ArgumentException("Expected audio map value (e.g. {type:\"audio\", uri:\"...\"}).");
            }

            var map = value.AsMapOrEmpty();
            if (!map.TryGetValue("uri", out var uriValue))
            {
                throw new ArgumentException("Audio has no 'uri' field.");
            }

            var uri = uriValue.AsString();
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentException("Audio has no URI");
            }

            uri = NormalizeAssetUri(uri, apiBaseUrl);
            return await AssetManager.DownloadAssetAsync(uri);
        }
    }
}
