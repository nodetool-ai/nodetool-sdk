using Nodetool.SDK.Types.Assets;
using Nodetool.SDK.VL.Services;
using Nodetool.SDK.VL.Utilities;
using VL.Core;
using VL.Core.CompilerServices;
using VlPath = VL.Lib.IO.Path;

namespace Nodetool.SDK.VL.Factories;

internal static class AssetNodeFactory
{
    internal static IVLNodeDescription? CreateUploadAssetNode(
        IVLNodeDescriptionFactory vlSelfFactory)
    {
        return vlSelfFactory.NewNodeDescription(
            name: "UploadAsset",
            category: "Nodetool.Assets",
            fragmented: false,
            bc =>
            {
                var pathPin = bc.Pin(
                    "Path",
                    typeof(VlPath),
                    new VlPath(""),
                    "Local asset file",
                    "The file is uploaded only when Upload receives a rising edge.");
                var uploadPin = bc.Pin(
                    "Upload",
                    typeof(bool),
                    false,
                    "Upload file",
                    "Trigger on a rising edge.");
                var temporaryPin = bc.Pin(
                    "Temporary",
                    typeof(bool),
                    true,
                    "Temporary upload",
                    "Temporary assets avoid database and thumbnail work and are intended for execution inputs.");
                var contentTypePin = new VlPinDescription(
                    "ContentType",
                    typeof(string),
                    "",
                    "MIME type override",
                    "Leave empty to infer the MIME type from the file extension.",
                    isVisible: false);
                var assetPin = bc.Pin(
                    "Asset",
                    typeof(AssetRef),
                    default(AssetRef),
                    "Uploaded asset",
                    "A temporary or persistent typed NodeTool asset reference.");
                var uploadingPin = bc.Pin(
                    "IsUploading",
                    typeof(bool),
                    false,
                    "Upload in progress",
                    "True until the current upload completes.");
                var successPin = bc.Pin(
                    "Success",
                    typeof(bool),
                    false,
                    "Upload succeeded",
                    "Remains true for the latest successful upload.");
                var errorPin = new VlPinDescription(
                    "Error",
                    typeof(string),
                    "",
                    "Upload error",
                    "Empty after a successful upload.",
                    isVisible: false);

                return bc.Node(
                    inputs: new[]
                    {
                        pathPin, temporaryPin, contentTypePin, uploadPin
                    },
                    outputs: new[]
                    {
                        assetPin, uploadingPin, successPin, errorPin
                    },
                    newNode: ibc =>
                    {
                        var stateLock = new object();
                        string localPath = "";
                        string contentType = "";
                        bool temporary = true;
                        bool lastUpload = false;
                        AssetRef? asset = null;
                        bool isUploading = false;
                        bool success = false;
                        string error = "";
                        long requestVersion = 0;
                        CancellationTokenSource? requestCancellation = null;

                        void StartUpload()
                        {
                            var requestedPath = localPath;
                            var requestedContentType = contentType;
                            var requestedTemporary = temporary;
                            requestCancellation?.Cancel();
                            requestCancellation?.Dispose();
                            requestCancellation = new CancellationTokenSource();
                            var cancellationToken = requestCancellation.Token;
                            var version = Interlocked.Increment(
                                ref requestVersion);
                            lock (stateLock)
                            {
                                isUploading = true;
                                success = false;
                                error = "";
                            }

                            _ = RunUploadAsync();
                            async Task RunUploadAsync()
                            {
                                try
                                {
                                    var result =
                                        await AssetTransferService.UploadAsync(
                                            requestedPath,
                                            requestedContentType,
                                            requestedTemporary,
                                            cancellationToken);
                                    if (version != Interlocked.Read(
                                        ref requestVersion))
                                    {
                                        return;
                                    }
                                    lock (stateLock)
                                    {
                                        asset = result;
                                        isUploading = false;
                                        success = true;
                                    }
                                }
                                catch (OperationCanceledException)
                                    when (cancellationToken
                                        .IsCancellationRequested)
                                {
                                    // A newer upload superseded this request.
                                }
                                catch (Exception ex)
                                {
                                    if (version != Interlocked.Read(
                                        ref requestVersion))
                                    {
                                        return;
                                    }
                                    lock (stateLock)
                                    {
                                        asset = null;
                                        isUploading = false;
                                        success = false;
                                        error = VlLog.SafeError(ex);
                                    }
                                }
                            }
                        }

                        return ibc.Node(
                            inputs: new IVLPin[]
                            {
                                ibc.Input<VlPath>(value =>
                                    localPath = value?.ToString() ?? ""),
                                ibc.Input<bool>(value => temporary = value),
                                ibc.Input<string>(value =>
                                    contentType = value ?? ""),
                                ibc.Input<bool>(value =>
                                {
                                    if (value && !lastUpload)
                                        StartUpload();
                                    lastUpload = value;
                                })
                            },
                            outputs: new IVLPin[]
                            {
                                ibc.Output<AssetRef?>(() =>
                                {
                                    lock (stateLock) return asset;
                                }),
                                ibc.Output<bool>(() =>
                                {
                                    lock (stateLock) return isUploading;
                                }),
                                ibc.Output<bool>(() =>
                                {
                                    lock (stateLock) return success;
                                }),
                                ibc.Output<string>(() =>
                                {
                                    lock (stateLock) return error;
                                })
                            });
                    },
                    summary: "Upload a local file as a NodeTool asset",
                    remarks:
                        "Temporary mode is the fast default for reusable execution inputs. "
                        + "Disable it only when the upload should become a durable NodeTool library asset.");
            });
    }

    internal static IVLNodeDescription? CreateSaveAssetNode(
        IVLNodeDescriptionFactory vlSelfFactory)
    {
        return vlSelfFactory.NewNodeDescription(
            name: "SaveAsset",
            category: "Nodetool.Assets",
            fragmented: false,
            bc =>
            {
                var assetPin = bc.Pin(
                    "Asset",
                    typeof(AssetRef),
                    default(AssetRef),
                    "Typed NodeTool asset",
                    "Accepts temporary, persistent, inline, local, or remote asset references.");
                var destinationPin = bc.Pin(
                    "Destination",
                    typeof(VlPath),
                    new VlPath(""),
                    "Destination file or directory",
                    "A missing extension is copied from the materialized source. A directory uses the asset name.");
                var savePin = bc.Pin(
                    "Save",
                    typeof(bool),
                    false,
                    "Save asset",
                    "Trigger on a rising edge.");
                var overwritePin = bc.Pin(
                    "Overwrite",
                    typeof(bool),
                    false,
                    "Replace existing file",
                    "When false, saving fails if the destination already exists.");
                var pathPin = bc.Pin(
                    "Path",
                    typeof(VlPath),
                    new VlPath(""),
                    "Saved file",
                    "The resulting local file path.");
                var savingPin = bc.Pin(
                    "IsSaving",
                    typeof(bool),
                    false,
                    "Save in progress",
                    "True while the asset is downloading or copying.");
                var successPin = bc.Pin(
                    "Success",
                    typeof(bool),
                    false,
                    "Save succeeded",
                    "Remains true for the latest successful save.");
                var errorPin = new VlPinDescription(
                    "Error",
                    typeof(string),
                    "",
                    "Save error",
                    "Empty after a successful save.",
                    isVisible: false);

                return bc.Node(
                    inputs: new[]
                    {
                        assetPin, destinationPin, overwritePin, savePin
                    },
                    outputs: new[]
                    {
                        pathPin, savingPin, successPin, errorPin
                    },
                    newNode: ibc =>
                    {
                        var stateLock = new object();
                        AssetRef? currentAsset = null;
                        string destination = "";
                        bool overwrite = false;
                        bool lastSave = false;
                        VlPath path = new("");
                        bool isSaving = false;
                        bool success = false;
                        string error = "";
                        long requestVersion = 0;
                        CancellationTokenSource? requestCancellation = null;

                        void StartSave()
                        {
                            var requestedAsset = currentAsset;
                            var requestedDestination = destination;
                            var requestedOverwrite = overwrite;
                            requestCancellation?.Cancel();
                            requestCancellation?.Dispose();
                            requestCancellation = new CancellationTokenSource();
                            var cancellationToken = requestCancellation.Token;
                            var version = Interlocked.Increment(
                                ref requestVersion);
                            lock (stateLock)
                            {
                                isSaving = true;
                                success = false;
                                error = "";
                            }

                            _ = RunSaveAsync();
                            async Task RunSaveAsync()
                            {
                                try
                                {
                                    if (requestedAsset == null ||
                                        requestedAsset.IsEmpty())
                                    {
                                        throw new InvalidOperationException(
                                            "Asset is empty.");
                                    }
                                    var result =
                                        await AssetTransferService.SaveAsync(
                                            requestedAsset,
                                            requestedDestination,
                                            requestedOverwrite,
                                            cancellationToken);
                                    if (version != Interlocked.Read(
                                        ref requestVersion))
                                    {
                                        return;
                                    }
                                    lock (stateLock)
                                    {
                                        path = new VlPath(result.Path);
                                        isSaving = false;
                                        success = true;
                                    }
                                }
                                catch (OperationCanceledException)
                                    when (cancellationToken
                                        .IsCancellationRequested)
                                {
                                    // A newer save superseded this request.
                                }
                                catch (Exception ex)
                                {
                                    if (version != Interlocked.Read(
                                        ref requestVersion))
                                    {
                                        return;
                                    }
                                    lock (stateLock)
                                    {
                                        path = new VlPath("");
                                        isSaving = false;
                                        success = false;
                                        error = VlLog.SafeError(ex);
                                    }
                                }
                            }
                        }

                        return ibc.Node(
                            inputs: new IVLPin[]
                            {
                                ibc.Input<AssetRef?>(value =>
                                    currentAsset = value),
                                ibc.Input<VlPath>(value =>
                                    destination =
                                        value?.ToString() ?? ""),
                                ibc.Input<bool>(value => overwrite = value),
                                ibc.Input<bool>(value =>
                                {
                                    if (value && !lastSave)
                                        StartSave();
                                    lastSave = value;
                                })
                            },
                            outputs: new IVLPin[]
                            {
                                ibc.Output<VlPath>(() =>
                                {
                                    lock (stateLock) return path;
                                }),
                                ibc.Output<bool>(() =>
                                {
                                    lock (stateLock) return isSaving;
                                }),
                                ibc.Output<bool>(() =>
                                {
                                    lock (stateLock) return success;
                                }),
                                ibc.Output<string>(() =>
                                {
                                    lock (stateLock) return error;
                                })
                            });
                    },
                    summary: "Save a NodeTool asset to a local file",
                    remarks:
                        "Materializes through the shared SDK cache and atomically copies to the selected destination.");
            });
    }

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
                                        error = VlLog.SafeError(ex);
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
