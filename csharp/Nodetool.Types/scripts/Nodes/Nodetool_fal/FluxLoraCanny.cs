using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_fal;

[MessagePackObject]
public class FluxLoraCanny
{
    [Key(0)]
    public Nodetool.Types.ImageRef control_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public object image_size { get; set; } = "ImageSizePreset.LANDSCAPE_4_3";
    [Key(3)]
    public int num_inference_steps { get; set; } = 28;
    [Key(4)]
    public double guidance_scale { get; set; } = 3.5;
    [Key(5)]
    public int seed { get; set; } = -1;
    [Key(6)]
    public double lora_scale { get; set; } = 0.6;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
