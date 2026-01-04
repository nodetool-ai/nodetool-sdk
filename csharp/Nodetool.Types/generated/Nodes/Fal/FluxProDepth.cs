using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FluxProDepth
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef control_image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(1)]
    public double guidance_scale { get; set; } = 3.5;
    [Key(2)]
    public object image_size { get; set; } = @"landscape_4_3";
    [Key(3)]
    public int num_inference_steps { get; set; } = 28;
    [Key(4)]
    public string prompt { get; set; } = @"";
    [Key(5)]
    public string safety_tolerance { get; set; } = @"2";
    [Key(6)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
