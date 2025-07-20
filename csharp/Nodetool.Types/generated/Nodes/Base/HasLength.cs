using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class HasLength
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public object min_length { get; set; } = null;
    [Key(2)]
    public object max_length { get; set; } = null;
    [Key(3)]
    public object exact_length { get; set; } = null;

    public bool Process()
    {
        return default(bool);
    }
}
