using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FastLightningSDXL
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE_HD";
    [Key(2)]
    public int num_inference_steps { get; set; } = 4;
    [Key(3)]
    public int seed { get; set; } = -1;
    [Key(4)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(5)]
    public bool expand_prompt { get; set; } = false;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
