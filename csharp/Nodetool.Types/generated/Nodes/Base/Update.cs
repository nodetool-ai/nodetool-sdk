using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Dictionary;

[MessagePackObject]
public class Update
{
    [Key(0)]
    public object dictionary { get; set; } = new Dictionary<string, object>();
    [Key(1)]
    public object new_pairs { get; set; } = new Dictionary<string, object>();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
