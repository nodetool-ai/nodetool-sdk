using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FluxProFill
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(1)]
    public Nodetool.Types.Core.ImageRef mask { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public int num_inference_steps { get; set; } = 28;
    [Key(3)]
    public string prompt { get; set; } = @"";
    [Key(4)]
    public string safety_tolerance { get; set; } = @"2";
    [Key(5)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
