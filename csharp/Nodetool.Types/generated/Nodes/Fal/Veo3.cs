using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class Veo3
{
    [Key(0)]
    public object aspect_ratio { get; set; }
    [Key(1)]
    public object duration { get; set; }
    [Key(2)]
    public bool enhance_prompt { get; set; } = true;
    [Key(3)]
    public bool generate_audio { get; set; } = true;
    [Key(4)]
    public string negative_prompt { get; set; } = @"";
    [Key(5)]
    public string prompt { get; set; } = @"";
    [Key(6)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
