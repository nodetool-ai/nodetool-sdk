using System.Collections.Immutable;
using Nodetool.SDK.Api;
using Nodetool.SDK.Assets;
using Nodetool.SDK.VL.Services;
using VL.Core;
using VL.Core.CompilerServices;

namespace Nodetool.SDK.VL.Factories;

/// <summary>
/// Asset utility nodes for VL: resolve/download, cache inspection, upload-from-path.
/// </summary>
internal static class AssetNodeFactory
{
    public static NodeBuilding.FactoryImpl GetFactory(IVLNodeDescriptionFactory vlSelfFactory)
    {
        if (vlSelfFactory == null)
            return NodeBuilding.NewFactoryImpl(ImmutableArray<IVLNodeDescription>.Empty);

        var nodes = new List<IVLNodeDescription>();

        Add(nodes, CreateResolveLocalPathNode(vlSelfFactory));
        Add(nodes, CreateGetCachedPathNode(vlSelfFactory));
        Add(nodes, CreateUploadFromPathNode(vlSelfFactory));
        Add(nodes, CreateCacheInfoNode(vlSelfFactory));
        Add(nodes, CreateClearCacheNode(vlSelfFactory));

        return NodeBuilding.NewFactoryImpl(ImmutableArray.CreateRange(nodes));
    }

    private static void Add(List<IVLNodeDescription> list, IVLNodeDescription? node)
    {
        if (node != null) list.Add(node);
    }

    private static IVLNodeDescription? CreateResolveLocalPathNode(IVLNodeDescriptionFactory f)
    {
        return f.NewNodeDescription(
            name: "ResolveLocalPath",
            category: "Nodetool.Assets",
            fragmented: false,
            bc =>
            {
                var uriPin = bc.Pin("UriOrPath", typeof(string), "",
                    "URI or path", "Asset URI (http(s), data:, file://, /api/...) or local file path.");
                var triggerPin = bc.Pin("Trigger", typeof(bool), false,
                    "Trigger", "Set true to resolve/download.");

                var localPathOut = bc.Pin("LocalPath", typeof(string), "",
                    "Local path", "Local file path (cached/downloaded).");
                var isCachedOut = bc.Pin("IsCached", typeof(bool), false,
                    "Cached", "True if asset was already in local cache (no download).");
                var errorOut = bc.Pin("Error", typeof(string), "",
                    "Error", "Error message (empty when ok).");

                return bc.Node(
                    inputs: new[] { uriPin, triggerPin },
                    outputs: new[] { localPathOut, isCachedOut, errorOut },
                    newNode: ibc =>
                    {
                        string uriOrPath = "";
                        bool trigger = false;
                        bool lastTrigger = false;

                        string localPath = "";
                        bool isCached = false;
                        string error = "";

                        async Task RunAsync()
                        {
                            error = "";
                            isCached = false;
                            try
                            {
                                var s = (uriOrPath ?? "").Trim();
                                if (string.IsNullOrWhiteSpace(s))
                                {
                                    localPath = "";
                                    return;
                                }

                                var normalized = NodeToolClientProvider.NormalizeAssetUri(s);
                                var assets = NodeToolClientProvider.GetAssetManager();
                                var hit = assets.GetCachedPath(normalized);
                                if (!string.IsNullOrWhiteSpace(hit) && File.Exists(hit))
                                {
                                    isCached = true;
                                    localPath = hit;
                                    return;
                                }

                                localPath = await assets.DownloadAssetAsync(normalized);
                            }
                            catch (Exception ex)
                            {
                                error = ex.Message;
                            }
                        }

                        return ibc.Node(
                            inputs: new IVLPin[]
                            {
                                ibc.Input<string>(v => uriOrPath = v ?? ""),
                                ibc.Input<bool>(v =>
                                {
                                    trigger = v;
                                    if (trigger && !lastTrigger)
                                        _ = RunAsync();
                                    lastTrigger = trigger;
                                }),
                            },
                            outputs: new IVLPin[]
                            {
                                ibc.Output<string>(() => localPath),
                                ibc.Output<bool>(() => isCached),
                                ibc.Output<string>(() => error),
                            }
                        );
                    },
                    summary: "Resolve a NodeTool asset URI/path into a local cached file path",
                    remarks: "Downloads assets into the configured cache directory (see Nodetool.Connect.AssetCacheDir)."
                );
            }
        );
    }

    private static IVLNodeDescription? CreateGetCachedPathNode(IVLNodeDescriptionFactory f)
    {
        return f.NewNodeDescription(
            name: "GetCachedPath",
            category: "Nodetool.Assets",
            fragmented: false,
            bc =>
            {
                var uriPin = bc.Pin("UriOrPath", typeof(string), "",
                    "URI or path", "Asset URI (http(s), data:, file://, /api/...) or local file path.");

                var localPathOut = bc.Pin("LocalPath", typeof(string), "",
                    "Local path", "Local cached file path (empty if not cached).");

                return bc.Node(
                    inputs: new[] { uriPin },
                    outputs: new[] { localPathOut },
                    newNode: ibc =>
                    {
                        string uriOrPath = "";
                        string localPath = "";

                        void Recompute()
                        {
                            var s = (uriOrPath ?? "").Trim();
                            if (string.IsNullOrWhiteSpace(s))
                            {
                                localPath = "";
                                return;
                            }

                            var normalized = NodeToolClientProvider.NormalizeAssetUri(s);
                            var assets = NodeToolClientProvider.GetAssetManager();
                            var hit = assets.GetCachedPath(normalized);
                            localPath = (!string.IsNullOrWhiteSpace(hit) && File.Exists(hit)) ? hit : "";
                        }

                        return ibc.Node(
                            inputs: new IVLPin[] { ibc.Input<string>(v => { uriOrPath = v ?? ""; Recompute(); }) },
                            outputs: new IVLPin[] { ibc.Output<string>(() => localPath) }
                        );
                    },
                    summary: "Get cached local path for an asset (no download)",
                    remarks: "Returns empty if the asset is not in the local cache."
                );
            }
        );
    }

    private static IVLNodeDescription? CreateUploadFromPathNode(IVLNodeDescriptionFactory f)
    {
        return f.NewNodeDescription(
            name: "UploadFromPath",
            category: "Nodetool.Assets",
            fragmented: false,
            bc =>
            {
                var pathPin = bc.Pin("LocalPath", typeof(string), "",
                    "Local path", "Path to a local file to upload as an asset.");
                var triggerPin = bc.Pin("Trigger", typeof(bool), false, "Trigger", "Set true to upload.");

                var uriOut = bc.Pin("AssetUri", typeof(string), "", "Asset URI", "Absolute download URL.");
                var idOut = bc.Pin("AssetId", typeof(string), "", "Asset ID", "Server-assigned asset id.");
                var errorOut = bc.Pin("Error", typeof(string), "", "Error", "Error message (empty when ok).");

                return bc.Node(
                    inputs: new[] { pathPin, triggerPin },
                    outputs: new[] { uriOut, idOut, errorOut },
                    newNode: ibc =>
                    {
                        string localPath = "";
                        bool trigger = false;
                        bool lastTrigger = false;

                        string assetUri = "";
                        string assetId = "";
                        string error = "";

                        async Task RunAsync()
                        {
                            error = "";
                            try
                            {
                                var apiBase = NodeToolClientProvider.CurrentApiBaseUrl;
                                if (apiBase == null)
                                    throw new InvalidOperationException("No API base URL. Connect first.");

                                var token = NodeToolClientProvider.CurrentAuthToken;
                                var client = new NodetoolClient();
                                client.Configure(apiBase.ToString().TrimEnd('/'), apiKey: token);

                                var result = await AssetUploader.UploadFromPathAsync(client, apiBase, localPath);
                                assetUri = result.Uri; // already absolute
                                assetId = result.AssetId ?? "";
                            }
                            catch (Exception ex)
                            {
                                error = ex.Message;
                            }
                        }

                        return ibc.Node(
                            inputs: new IVLPin[]
                            {
                                ibc.Input<string>(v => localPath = v ?? ""),
                                ibc.Input<bool>(v =>
                                {
                                    trigger = v;
                                    if (trigger && !lastTrigger)
                                        _ = RunAsync();
                                    lastTrigger = trigger;
                                }),
                            },
                            outputs: new IVLPin[]
                            {
                                ibc.Output<string>(() => assetUri),
                                ibc.Output<string>(() => assetId),
                                ibc.Output<string>(() => error),
                            }
                        );
                    },
                    summary: "Upload a local file and return an ABSOLUTE asset download URL",
                    remarks: "This node uploads via the HTTP API derived from your Nodetool.Connect BaseUrl."
                );
            }
        );
    }

    private static IVLNodeDescription? CreateCacheInfoNode(IVLNodeDescriptionFactory f)
    {
        return f.NewNodeDescription(
            name: "CacheInfo",
            category: "Nodetool.Assets",
            fragmented: false,
            bc =>
            {
                var dirOut = bc.Pin("CacheDir", typeof(string), "", "Cache dir", "Current cache directory path.");
                var sizeOut = bc.Pin("CacheSizeBytes", typeof(long), 0L, "Cache size", "Cache size in bytes.");

                return bc.Node(
                    inputs: Array.Empty<IVLPinDescription>(),
                    outputs: new[] { dirOut, sizeOut },
                    newNode: ibc =>
                    {
                        return ibc.Node(
                            inputs: Array.Empty<IVLPin>(),
                            outputs: new IVLPin[]
                            {
                                ibc.Output<string>(() => NodeToolClientProvider.GetAssetManager().CacheDirectory),
                                ibc.Output<long>(() => NodeToolClientProvider.GetAssetManager().GetCacheSize()),
                            }
                        );
                    },
                    summary: "Inspect the NodeTool local asset cache",
                    remarks: "Shows where assets are cached on disk and the total cache size."
                );
            }
        );
    }

    private static IVLNodeDescription? CreateClearCacheNode(IVLNodeDescriptionFactory f)
    {
        return f.NewNodeDescription(
            name: "ClearCache",
            category: "Nodetool.Assets",
            fragmented: false,
            bc =>
            {
                var triggerPin = bc.Pin("Trigger", typeof(bool), false, "Trigger", "Set true to clear cache.");
                var okOut = bc.Pin("Ok", typeof(bool), false, "Ok", "True when cache was cleared without exception.");
                var errorOut = bc.Pin("Error", typeof(string), "", "Error", "Error message (empty when ok).");

                return bc.Node(
                    inputs: new[] { triggerPin },
                    outputs: new[] { okOut, errorOut },
                    newNode: ibc =>
                    {
                        bool trigger = false;
                        bool lastTrigger = false;
                        bool ok = false;
                        string error = "";

                        void Run()
                        {
                            ok = false;
                            error = "";
                            try
                            {
                                NodeToolClientProvider.GetAssetManager().ClearCache();
                                ok = true;
                            }
                            catch (Exception ex)
                            {
                                error = ex.Message;
                            }
                        }

                        return ibc.Node(
                            inputs: new IVLPin[]
                            {
                                ibc.Input<bool>(v =>
                                {
                                    trigger = v;
                                    if (trigger && !lastTrigger)
                                        Run();
                                    lastTrigger = trigger;
                                }),
                            },
                            outputs: new IVLPin[]
                            {
                                ibc.Output<bool>(() => ok),
                                ibc.Output<string>(() => error),
                            }
                        );
                    },
                    summary: "Clear the local NodeTool asset cache",
                    remarks: "Deletes cached files under the cache directory."
                );
            }
        );
    }
}


