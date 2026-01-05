using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CollectionInput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public Nodetool.Types.Core.Collection value { get; set; } = new Nodetool.Types.Core.Collection();

    public Nodetool.Types.Core.Collection Process()
    {
        return default(Nodetool.Types.Core.Collection);
    }
}
