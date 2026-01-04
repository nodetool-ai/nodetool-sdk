using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FilterRegex
{
    [Key(0)]
    public bool full_match { get; set; } = false;
    [Key(1)]
    public string pattern { get; set; } = @"";
    [Key(2)]
    public object values { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
