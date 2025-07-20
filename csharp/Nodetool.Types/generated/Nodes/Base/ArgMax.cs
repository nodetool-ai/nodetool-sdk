using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ArgMax
{
    [Key(0)]
    public object scores { get; set; } = new Dictionary<string, object>();

    public string Process()
    {
        return default(string);
    }
}
