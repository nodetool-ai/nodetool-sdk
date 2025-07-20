using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class GetJSONPathInt
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public string path { get; set; } = "";
    [Key(2)]
    public int default { get; set; } = 0;

    public int Process()
    {
        // Implementation would be generated based on node logic
        return default(int);
    }
}
