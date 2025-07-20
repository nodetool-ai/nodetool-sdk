using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ArrayOutput
{
    [Key(0)]
    public Nodetool.Types.NPArray value { get; set; } = new Nodetool.Types.NPArray();
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public Nodetool.Types.NPArray Process()
    {
        return default(Nodetool.Types.NPArray);
    }
}
