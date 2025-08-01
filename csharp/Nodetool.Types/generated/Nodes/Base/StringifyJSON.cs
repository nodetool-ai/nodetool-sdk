using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class StringifyJSON
{
    [Key(0)]
    public object data { get; set; } = new Dictionary<string, object>();
    [Key(1)]
    public int indent { get; set; } = 2;

    public string Process()
    {
        return default(string);
    }
}
