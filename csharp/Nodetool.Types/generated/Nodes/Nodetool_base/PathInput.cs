using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class PathInput
{
    [Key(0)]
    public Nodetool.Types.FilePath value { get; set; } = new Nodetool.Types.FilePath();
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public Nodetool.Types.FilePath Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.FilePath);
    }
}
