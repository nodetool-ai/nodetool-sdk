using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_fal;

[MessagePackObject]
public class FluxProFill
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public Nodetool.Types.ImageRef mask { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public string prompt { get; set; } = "";
    [Key(3)]
    public int num_inference_steps { get; set; } = 28;
    [Key(4)]
    public int seed { get; set; } = -1;
    [Key(5)]
    public string safety_tolerance { get; set; } = "2";

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
