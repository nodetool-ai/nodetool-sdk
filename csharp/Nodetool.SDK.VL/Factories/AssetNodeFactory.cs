using Nodetool.SDK.Types.Assets;
using Nodetool.SDK.VL.Services;
using VL.Core;
using VL.Core.CompilerServices;
using VlPath = VL.Lib.IO.Path;

namespace Nodetool.SDK.VL.Factories;

internal static class AssetNodeFactory
{
    internal static IVLNodeDescription? CreateAssetAsFileNode(
        IVLNodeDescriptionFactory vlSelfFactory)
    {
        return vlSelfFactory.NewNodeDescription(
            name: "AssetAsFile",
            category: "Nodetool.Assets",
            fragmented: false,
            bc =>
            {
                var assetPin = bc.Pin(
                    "Asset",
                    typeof(AssetRef),
                    default(AssetRef),
                    "Typed NodeTool asset",
                    "Accepts image, audio, video, document, text, font, model, or generic file asset references.");
                var refreshPin = bc.Pin(
                    "Refresh",
                    typeof(bool),
                    false,
                    "Refresh cached file",
                    "Trigger on a rising edge to download or rewrite the cached file again.");
                var pathPin = bc.Pin(
                    "Path",
                    typeof(VlPath),
                    new VlPath(""),
                    "Local file path",
                    "Existing local source path or the downloaded/cached file path.");
                var contentTypePin = bc.Pin(
                    "ContentType",
                    typeof(string),
                    "",
                    "Content type",
                    "MIME type reported by NodeTool, the HTTP response, or the file extension.");
                var sourceUriPin = bc.Pin(
                    "SourceUri",
                    typeof(string),
                    "",
                    "Resolved source URI",
                    "The file, HTTP, storage, or asset URI used to materialize the file.");
                var readyPin = bc.Pin(
                    "IsReady",
                    typeof(bool),
                    false,
                    "File ready",
                    "True when Path points to a materialized file.");
                var loadingPin = bc.Pin(
                    "IsLoading",
                    typeof(bool),
                    false,
                    "File loading",
                    "True while an inline asset is being written or a remote asset is downloading.");
                var cachedPin = bc.Pin(
                    "FromCache",
                    typeof(bool),
                    false,
                    "Cache hit",
                    "True when an existing cached file was reused.");
                var errorPin = bc.Pin(
                    "Error",
                    typeof(string),
                    "",
                    "Materialization error",
                    "Empty when the asset was materialized successfully.");

                return bc.Node(
                    inputs: new[] { assetPin, refreshPin },
                    outputs: new[]
                    {
                        pathPin, contentTypePin, sourceUriPin, readyPin,
                        loadingPin, cachedPin, errorPin
                    },
                    newNode: ibc =>
                    {
                        var stateLock = new object();
                        AssetRef? currentAsset = null;
                        string currentIdentity = "";
                        bool lastRefresh = false;
                        long requestVersion = 0;
                        VlPath path = new("");
                        string contentType = "";
                        string sourceUri = "";
                        bool isReady = false;
                        bool isLoading = false;
                        bool fromCache = false;
                        string error = "";
                        CancellationTokenSource? requestCancellation = null;

                        void StartMaterialization(bool forceRefresh)
                        {
                            var asset = currentAsset;
                            if (asset == null || asset.IsEmpty())
                            {
                                requestCancellation?.Cancel();
                                requestCancellation?.Dispose();
                                requestCancellation = null;
                                lock (stateLock)
                                {
                                    requestVersion++;
                                    path = new VlPath("");
                                    contentType = "";
                                    sourceUri = "";
                                    isReady = false;
                                    isLoading = false;
                                    fromCache = false;
                                    error = "";
                                }
                                return;
                            }

                            requestCancellation?.Cancel();
                            requestCancellation?.Dispose();
                            requestCancellation = new CancellationTokenSource();
                            var cancellationToken = requestCancellation.Token;
                            var version = Interlocked.Increment(ref requestVersion);
                            lock (stateLock)
                            {
                                isLoading = true;
                                isReady = false;
                                fromCache = false;
                                error = "";
                            }

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var result = await AssetFileMaterializer.MaterializeAsync(
                                        asset,
                                        forceRefresh,
                                        cancellationToken);
                                    if (version != Interlocked.Read(ref requestVersion))
                                        return;
                                    lock (stateLock)
                                    {
                                        path = new VlPath(result.Path);
                                        contentType = result.ContentType;
                                        sourceUri = result.SourceUri;
                                        fromCache = result.FromCache;
                                        isReady = File.Exists(result.Path);
                                        isLoading = false;
                                    }
                                }
                                catch (OperationCanceledException)
                                    when (cancellationToken.IsCancellationRequested)
                                {
                                    // A newer input or refresh superseded this request.
                                }
                                catch (Exception ex)
                                {
                                    if (version != Interlocked.Read(ref requestVersion))
                                        return;
                                    lock (stateLock)
                                    {
                                        path = new VlPath("");
                                        contentType = "";
                                        sourceUri = "";
                                        fromCache = false;
                                        isReady = false;
                                        isLoading = false;
                                        error = ex.Message;
                                    }
                                }
                            });
                        }

                        return ibc.Node(
                            inputs: new IVLPin[]
                            {
                                ibc.Input<AssetRef?>(asset =>
                                {
                                    currentAsset = asset;
                                    var identity = CreateInputIdentity(asset);
                                    if (!string.Equals(identity, currentIdentity, StringComparison.Ordinal))
                                    {
                                        currentIdentity = identity;
                                        StartMaterialization(forceRefresh: false);
                                    }
                                }),
                                ibc.Input<bool>(refresh =>
                                {
                                    if (refresh && !lastRefresh)
                                        StartMaterialization(forceRefresh: true);
                                    lastRefresh = refresh;
                                })
                            },
                            outputs: new IVLPin[]
                            {
                                ibc.Output<VlPath>(() => { lock (stateLock) return path; }),
                                ibc.Output<string>(() => { lock (stateLock) return contentType; }),
                                ibc.Output<string>(() => { lock (stateLock) return sourceUri; }),
                                ibc.Output<bool>(() => { lock (stateLock) return isReady; }),
                                ibc.Output<bool>(() => { lock (stateLock) return isLoading; }),
                                ibc.Output<bool>(() => { lock (stateLock) return fromCache; }),
                                ibc.Output<string>(() => { lock (stateLock) return error; })
                            });
                    },
                    summary: "Materialize a typed NodeTool asset as a local file",
                    remarks:
                        "Passes through existing local files and caches inline, HTTP, /api/storage, asset://, "
                        + "and ID-only assets under the user's local NodeTool SDK cache.");
            });
    }

    private static string CreateInputIdentity(AssetRef? asset)
    {
        if (asset == null)
            return "";
        var dataIdentity = asset.Data switch
        {
            byte[] bytes => Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(bytes)),
            null => "",
            _ => $"{asset.Data.GetType().FullName}:{asset.Data.GetHashCode()}"
        };
        return $"{asset.Type}|{asset.AssetId}|{asset.TempId}|{asset.Uri}|{dataIdentity}";
    }
}
