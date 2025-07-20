using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class MMAudioV2
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public string negative_prompt { get; set; } = "";
    [Key(2)]
    public int num_steps { get; set; } = 25;
    [Key(3)]
    public double duration { get; set; } = 8.0;
    [Key(4)]
    public double cfg_strength { get; set; } = 4.5;
    [Key(5)]
    public bool mask_away_clip { get; set; } = false;
    [Key(6)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.AudioRef Process()
    {
        return default(Nodetool.Types.AudioRef);
    }
}
