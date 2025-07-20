using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FluxProUltraRedux
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public object image_size { get; set; } = "ImageSizePreset.LANDSCAPE_4_3";
    [Key(2)]
    public int num_inference_steps { get; set; } = 28;
    [Key(3)]
    public double guidance_scale { get; set; } = 3.5;
    [Key(4)]
    public int seed { get; set; } = -1;
    [Key(5)]
    public string safety_tolerance { get; set; } = "2";
    [Key(6)]
    public double image_prompt_strength { get; set; } = 0.1;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
