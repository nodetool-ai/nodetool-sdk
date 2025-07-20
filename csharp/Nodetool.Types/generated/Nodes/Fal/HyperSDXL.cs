using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class HyperSDXL
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE_HD";
    [Key(2)]
    public int num_inference_steps { get; set; } = 1;
    [Key(3)]
    public bool sync_mode { get; set; } = true;
    [Key(4)]
    public int num_images { get; set; } = 1;
    [Key(5)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(6)]
    public bool expand_prompt { get; set; } = false;
    [Key(7)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
