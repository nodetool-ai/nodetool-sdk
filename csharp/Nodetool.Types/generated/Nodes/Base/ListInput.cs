using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ListInput
{
    [Key(0)]
    public object value { get; set; } = new List<object>();
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
