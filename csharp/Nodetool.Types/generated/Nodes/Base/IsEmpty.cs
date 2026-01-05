using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IsEmpty
{
    [Key(0)]
    public string text { get; set; } = @"";
    [Key(1)]
    public bool trim_whitespace { get; set; } = true;

    public bool Process()
    {
        return default(bool);
    }
}
