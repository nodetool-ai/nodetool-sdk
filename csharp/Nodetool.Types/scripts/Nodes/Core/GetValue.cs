using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class GetValue
{
    [Key(0)]
    public object dictionary { get; set; } = new Dictionary<string, object>();
    [Key(1)]
    public string key { get; set; } = "";
    [Key(2)]
    public object default { get; set; } = null;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
