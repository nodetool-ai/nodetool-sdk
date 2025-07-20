using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class IdeogramV2Edit
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public Nodetool.Types.ImageRef mask { get; set; } = new Nodetool.Types.ImageRef();
    [Key(3)]
    public string style { get; set; } = "auto";
    [Key(4)]
    public bool expand_prompt { get; set; } = true;
    [Key(5)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
