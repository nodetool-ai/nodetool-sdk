using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FluxV1Pro
{
    [Key(0)]
    public double guidance_scale { get; set; } = 3.5;
    [Key(1)]
    public object image_size { get; set; } = @"square_hd";
    [Key(2)]
    public int num_inference_steps { get; set; } = 28;
    [Key(3)]
    public string prompt { get; set; } = @"";
    [Key(4)]
    public int? seed { get; set; } = null;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
