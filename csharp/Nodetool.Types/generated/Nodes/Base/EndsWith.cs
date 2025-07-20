using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class EndsWith
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string suffix { get; set; } = "";

    public bool Process()
    {
        return default(bool);
    }
}
