using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class GetEnvironmentVariable
{
    [Key(0)]
    public string name { get; set; } = "";
    [Key(1)]
    public object default { get; set; } = null;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
