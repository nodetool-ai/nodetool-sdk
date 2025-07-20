using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class SetEnvironmentVariable
{
    [Key(0)]
    public string name { get; set; } = "";
    [Key(1)]
    public string value { get; set; } = "";

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
