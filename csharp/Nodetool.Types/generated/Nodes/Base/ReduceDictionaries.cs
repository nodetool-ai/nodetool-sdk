using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ReduceDictionaries
{
    [Key(0)]
    public object dictionaries { get; set; } = new List<object>();
    [Key(1)]
    public string key_field { get; set; } = "";
    [Key(2)]
    public object value_field { get; set; } = null;
    [Key(3)]
    public object conflict_resolution { get; set; } = "ConflictResolution.FIRST";

    public object Process()
    {
        return default(object);
    }
}
