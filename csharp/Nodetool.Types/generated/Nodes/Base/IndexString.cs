using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IndexString
{
    [Key(0)]
    public Nodetool.Types.Core.Collection collection { get; set; } = new Nodetool.Types.Core.Collection();
    [Key(1)]
    public string document_id { get; set; } = @"";
    [Key(2)]
    public object metadata { get; set; } = new();
    [Key(3)]
    public string text { get; set; } = @"";

    public void Process()
    {
    }
}
