using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CollectionInput
{
    [Key(0)]
    public Nodetool.Types.Collection value { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public Nodetool.Types.Collection Process()
    {
        return default(Nodetool.Types.Collection);
    }
}
