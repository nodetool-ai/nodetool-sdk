using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ReduceDictionaries
{
    [Key(0)]
    public object conflict_resolution { get; set; } = @"first";
    [Key(1)]
    public object dictionaries { get; set; } = new();
    [Key(2)]
    public string key_field { get; set; } = @"";
    [Key(3)]
    public string value_field { get; set; } = null;

    public object Process()
    {
        return default(object);
    }
}
