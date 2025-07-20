using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetDocuments
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public object ids { get; set; } = new List<object>();
    [Key(2)]
    public int limit { get; set; } = 100;
    [Key(3)]
    public int offset { get; set; } = 0;

    public object Process()
    {
        return default(object);
    }
}
