using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class IllusionDiffusion
{
    [Key(0)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public object image_size { get; set; } = @"square_hd";
    [Key(3)]
    public string negative_prompt { get; set; } = @"";
    [Key(4)]
    public int num_inference_steps { get; set; } = 40;
    [Key(5)]
    public string prompt { get; set; } = @"";
    [Key(6)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
