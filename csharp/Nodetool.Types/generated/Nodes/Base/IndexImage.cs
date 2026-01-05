using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IndexImage
{
    [Key(0)]
    public Nodetool.Types.Core.Collection collection { get; set; } = new Nodetool.Types.Core.Collection();
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new();
    [Key(2)]
    public string index_id { get; set; } = @"";
    [Key(3)]
    public object metadata { get; set; } = new();
    [Key(4)]
    public bool upsert { get; set; } = false;

    public void Process()
    {
    }
}
