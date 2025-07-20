using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FilterRegex
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public string pattern { get; set; } = "";
    [Key(2)]
    public bool full_match { get; set; } = false;

    public object Process()
    {
        return default(object);
    }
}
