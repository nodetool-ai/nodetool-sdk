using System.Text.Json;
using VL.Core;
using VL.Core.CompilerServices;
using Nodetool.SDK.VL.Utilities;

namespace Nodetool.SDK.VL.Factories;

/// <summary>
/// Small utility nodes for dealing with ImageRef payloads.
/// </summary>
internal static class ImageNodeFactory
{
    internal static IVLNodeDescription? CreateDecodeImageRefNode(IVLNodeDescriptionFactory vlSelfFactory)
    {
        return vlSelfFactory.NewNodeDescription(
            name: "DecodeImageRef",
            category: "Nodetool",
            fragmented: false,
            bc =>
            {
                var valuePin = bc.Pin("Value", typeof(object), null,
                    "ImageRef value", "Accepts ImageRef JSON (string), a file path (string), or encoded image bytes (byte[]).");

                var bytesOut = bc.Pin("Bytes", typeof(byte[]), Array.Empty<byte>(),
                    "Encoded bytes", "The encoded image bytes (typically PNG/JPG).");
                var formatOut = bc.Pin("Format", typeof(string), "",
                    "Format", "Image format from metadata (e.g. png).");
                var widthOut = bc.Pin("Width", typeof(int), 0,
                    "Width", "Image width from metadata, if present.");
                var heightOut = bc.Pin("Height", typeof(int), 0,
                    "Height", "Image height from metadata, if present.");
                var errorOut = bc.Pin("Error", typeof(string), "",
                    "Error", "Decode error message (empty when ok).");

                return bc.Node(
                    inputs: new[] { valuePin },
                    outputs: new[] { bytesOut, formatOut, widthOut, heightOut, errorOut },
                    newNode: ibc =>
                    {
                        object? current = null;

                        byte[] bytes = Array.Empty<byte>();
                        string format = "";
                        int width = 0;
                        int height = 0;
                        string error = "";

                        void Recompute()
                        {
                            bytes = Array.Empty<byte>();
                            format = "";
                            width = 0;
                            height = 0;
                            error = "";

                            try
                            {
                                if (current == null)
                                    return;

                                if (current is byte[] directBytes)
                                {
                                    bytes = directBytes;
                                }
                                else if (current is string s)
                                {
                                    s = s.Trim();
                                    if (File.Exists(s))
                                    {
                                        bytes = File.ReadAllBytes(s);
                                    }
                                    else
                                    {
                                        // Try parse as ImageRef JSON
                                        if (TryParseImageRefJson(s, out var parsedBytes, out var meta, out var parseError))
                                        {
                                            bytes = parsedBytes ?? Array.Empty<byte>();
                                            if (meta.TryGetValue("format", out var fmt)) format = fmt;
                                            if (meta.TryGetValue("width", out var w) && int.TryParse(w, out var wi)) width = wi;
                                            if (meta.TryGetValue("height", out var h) && int.TryParse(h, out var hi)) height = hi;
                                        }
                                        else
                                        {
                                            error = parseError ?? "Not a file path and not valid ImageRef JSON.";
                                        }
                                    }
                                }
                                else
                                {
                                    // Last resort: try ToString() as JSON
                                    var jsonStr = current.ToString() ?? "";
                                    if (!TryParseImageRefJson(jsonStr, out var parsedBytes, out var meta, out var parseError))
                                        error = parseError ?? $"Unsupported Value type: {current.GetType().FullName}";
                                    else
                                    {
                                        bytes = parsedBytes ?? Array.Empty<byte>();
                                        if (meta.TryGetValue("format", out var fmt)) format = fmt;
                                        if (meta.TryGetValue("width", out var w) && int.TryParse(w, out var wi)) width = wi;
                                        if (meta.TryGetValue("height", out var h) && int.TryParse(h, out var hi)) height = hi;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                error = VlLog.SafeError(ex);
                            }
                        }

                        return ibc.Node(
                            inputs: new IVLPin[]
                            {
                                ibc.Input<object?>(val => { current = val; Recompute(); }),
                            },
                            outputs: new IVLPin[]
                            {
                                ibc.Output<byte[]>(() => bytes),
                                ibc.Output<string>(() => format),
                                ibc.Output<int>(() => width),
                                ibc.Output<int>(() => height),
                                ibc.Output<string>(() => error)
                            }
                        );
                    },
                    summary: "Decode an ImageRef payload to encoded bytes",
                    remarks: "Use this to turn workflow outputs like [{type:\"image\", data:[...]}] into encoded bytes"
                );
            }
        );
    }

    private static bool TryParseImageRefJson(
        string json,
        out byte[]? bytes,
        out Dictionary<string, string> metadata,
        out string? error)
    {
        bytes = null;
        metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        error = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement obj;
            if (root.ValueKind == JsonValueKind.Array)
            {
                if (root.GetArrayLength() == 0)
                {
                    error = "Empty array.";
                    return false;
                }
                obj = root[0];
            }
            else
            {
                obj = root;
            }

            if (obj.ValueKind != JsonValueKind.Object)
            {
                error = "Expected an object or an array of objects.";
                return false;
            }

            if (!obj.TryGetProperty("type", out var typeProp) || typeProp.GetString() is not string typeStr ||
                !string.Equals(typeStr, "image", StringComparison.OrdinalIgnoreCase))
            {
                error = "Not an ImageRef (missing type=image).";
                return false;
            }

            if (obj.TryGetProperty("metadata", out var metaProp) && metaProp.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in metaProp.EnumerateObject())
                {
                    if (p.Value.ValueKind == JsonValueKind.String)
                        metadata[p.Name] = p.Value.GetString() ?? "";
                    else if (p.Value.ValueKind == JsonValueKind.Number)
                        metadata[p.Name] = p.Value.GetRawText();
                }
            }

            if (obj.TryGetProperty("data", out var dataProp))
            {
                if (dataProp.ValueKind == JsonValueKind.Array)
                {
                    var tmp = new byte[dataProp.GetArrayLength()];
                    var i = 0;
                    foreach (var n in dataProp.EnumerateArray())
                    {
                        tmp[i++] = (byte)n.GetInt32();
                    }
                    bytes = tmp;
                }
            }

            // If bytes are still missing, allow uri to be a file:// path.
            if ((bytes == null || bytes.Length == 0) && obj.TryGetProperty("uri", out var uriProp) && uriProp.ValueKind == JsonValueKind.String)
            {
                var uriStr = uriProp.GetString() ?? "";
                if (Uri.TryCreate(uriStr, UriKind.Absolute, out var u) && u.IsFile && File.Exists(u.LocalPath))
                {
                    bytes = File.ReadAllBytes(u.LocalPath);
                }
            }

            if (bytes == null || bytes.Length == 0)
            {
                error = "ImageRef has no data bytes.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to parse JSON: {VlLog.SafeError(ex)}";
            return false;
        }
    }
}


