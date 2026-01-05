using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class IdeogramV2Edit
{
    [Key(0)]
    public bool expand_prompt { get; set; } = true;
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public Nodetool.Types.Core.ImageRef mask { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public string prompt { get; set; } = @"";
    [Key(4)]
    public int seed { get; set; } = -1;
    [Key(5)]
    public string style { get; set; } = @"auto";

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
