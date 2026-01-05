using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class PlaygroundV25
{
    [Key(0)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(1)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(2)]
    public object image_size { get; set; } = @"square_hd";
    [Key(3)]
    public int num_inference_steps { get; set; } = 30;
    [Key(4)]
    public string prompt { get; set; } = @"";
    [Key(5)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
