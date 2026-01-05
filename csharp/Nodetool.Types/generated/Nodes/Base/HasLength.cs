using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class HasLength
{
    [Key(0)]
    public int? exact_length { get; set; } = null;
    [Key(1)]
    public int? max_length { get; set; } = null;
    [Key(2)]
    public int? min_length { get; set; } = null;
    [Key(3)]
    public string text { get; set; } = @"";

    public bool Process()
    {
        return default(bool);
    }
}
