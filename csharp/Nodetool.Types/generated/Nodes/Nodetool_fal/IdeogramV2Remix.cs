using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_fal;

[MessagePackObject]
public class IdeogramV2Remix
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public string aspect_ratio { get; set; } = "1:1";
    [Key(3)]
    public double strength { get; set; } = 0.8;
    [Key(4)]
    public bool expand_prompt { get; set; } = true;
    [Key(5)]
    public string style { get; set; } = "auto";
    [Key(6)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
