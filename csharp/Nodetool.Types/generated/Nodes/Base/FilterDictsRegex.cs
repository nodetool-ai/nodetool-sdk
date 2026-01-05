using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FilterDictsRegex
{
    [Key(0)]
    public bool full_match { get; set; } = false;
    [Key(1)]
    public string key { get; set; } = @"";
    [Key(2)]
    public string pattern { get; set; } = @"";
    [Key(3)]
    public object values { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
