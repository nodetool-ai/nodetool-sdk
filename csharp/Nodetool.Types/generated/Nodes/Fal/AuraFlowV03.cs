using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class AuraFlowV03
{
    [Key(0)]
    public bool expand_prompt { get; set; } = true;
    [Key(1)]
    public double guidance_scale { get; set; } = 3.5;
    [Key(2)]
    public int num_images { get; set; } = 1;
    [Key(3)]
    public int num_inference_steps { get; set; } = 50;
    [Key(4)]
    public string prompt { get; set; } = @"";
    [Key(5)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
