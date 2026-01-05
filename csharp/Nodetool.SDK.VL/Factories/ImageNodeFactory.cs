using System.Collections.Immutable;
using System.Text.Json;
using SkiaSharp;
using VL.Core;
using VL.Core.CompilerServices;

namespace Nodetool.SDK.VL.Factories;

/// <summary>
/// Small utility nodes for dealing with ImageRef payloads.
/// </summary>
internal static class ImageNodeFactory
{
    public static NodeBuilding.FactoryImpl GetFactory(IVLNodeDescriptionFactory vlSelfFactory)
    {
        if (vlSelfFactory == null)
            return NodeBuilding.NewFactoryImpl(ImmutableArray<IVLNodeDescription>.Empty);

        var nodeDescriptions = new List<IVLNodeDescription>();
        var decode = CreateDecodeImageRefNode(vlSelfFactory);
        if (decode != null)
            nodeDescriptions.Add(decode);

        var decodeToImage = CreateDecodeImageRefToSKImageNode(vlSelfFactory);
        if (decodeToImage != null)
            nodeDescriptions.Add(decodeToImage);

        return NodeBuilding.NewFactoryImpl(ImmutableArray.CreateRange(nodeDescriptions));
    }

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
                                error = ex.Message;
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

    internal static IVLNodeDescription? CreateDecodeImageRefToSKImageNode(IVLNodeDescriptionFactory vlSelfFactory)
    {
        return vlSelfFactory.NewNodeDescription(
            name: "DecodeImageRefToSKImage",
            category: "Nodetool.Images",
            fragmented: false,
            bc =>
            {
                var valuePin = bc.Pin("Value", typeof(object), null,
                    "ImageRef value", "Accepts ImageRef JSON (string), a file path (string), or encoded image bytes (byte[]).");

                var imageOut = bc.Pin("Image", typeof(SKImage), default(SKImage),
                    "SKImage", "Decoded SkiaSharp SKImage (dispose handled by node).");
                var widthOut = bc.Pin("Width", typeof(int), 0, "Width", "Decoded image width.");
                var heightOut = bc.Pin("Height", typeof(int), 0, "Height", "Decoded image height.");
                var okOut = bc.Pin("IsValid", typeof(bool), false, "Valid", "True when decoding succeeded.");
                var errorOut = bc.Pin("Error", typeof(string), "", "Error", "Decode error message.");

                return bc.Node(
                    inputs: new[] { valuePin },
                    outputs: new[] { imageOut, widthOut, heightOut, okOut, errorOut },
                    newNode: ibc =>
                    {
                        object? current = null;
                        SKImage? image = null;
                        int width = 0;
                        int height = 0;
                        bool ok = false;
                        string error = "";

                        void Recompute()
                        {
                            ok = false;
                            error = "";
                            width = 0;
                            height = 0;

                            try
                            {
                                byte[] bytes;
                                if (current is byte[] direct)
                                {
                                    bytes = direct;
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
                                        if (!TryParseImageRefJson(s, out var parsedBytes, out _, out var parseError) || parsedBytes == null)
                                            throw new InvalidOperationException(parseError ?? "Not valid ImageRef JSON.");
                                        bytes = parsedBytes;
                                    }
                                }
                                else
                                {
                                    var jsonStr = current?.ToString() ?? "";
                                    if (!TryParseImageRefJson(jsonStr, out var parsedBytes, out _, out var parseError) || parsedBytes == null)
                                        throw new InvalidOperationException(parseError ?? "Unsupported value.");
                                    bytes = parsedBytes;
                                }

                                if (bytes.Length == 0)
                                    throw new InvalidOperationException("No image bytes.");

                                // Replace image (dispose previous)
                                image?.Dispose();
                                image = SKImage.FromEncodedData(bytes) ?? throw new InvalidOperationException("Failed to decode image bytes.");
                                width = image.Width;
                                height = image.Height;
                                ok = true;
                            }
                            catch (Exception ex)
                            {
                                image?.Dispose();
                                image = null;
                                error = ex.Message;
                                ok = false;
                            }
                        }

                        return ibc.Node(
                            inputs: new IVLPin[]
                            {
                                ibc.Input<object?>(val => { current = val; Recompute(); }),
                            },
                            outputs: new IVLPin[]
                            {
                                ibc.Output<SKImage>(() => image ?? default!),
                                ibc.Output<int>(() => width),
                                ibc.Output<int>(() => height),
                                ibc.Output<bool>(() => ok),
                                ibc.Output<string>(() => error),
                            }
                        );
                    },
                    summary: "Decode an ImageRef payload to a SkiaSharp SKImage",
                    remarks: "Uses SkiaSharp.SKImage.FromEncodedData on the encoded bytes from ImageRef.data"
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
            error = $"Failed to parse JSON: {ex.Message}";
            return false;
        }
    }
}


