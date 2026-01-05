using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class IdeogramV2Remix
{
    [Key(0)]
    public string aspect_ratio { get; set; } = @"1:1";
    [Key(1)]
    public bool expand_prompt { get; set; } = true;
    [Key(2)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public string prompt { get; set; } = @"";
    [Key(4)]
    public int seed { get; set; } = -1;
    [Key(5)]
    public double strength { get; set; } = 0.8;
    [Key(6)]
    public string style { get; set; } = @"auto";

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
