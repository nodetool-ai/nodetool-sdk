using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class RelativePath
{
    [Key(0)]
    public string target_path { get; set; } = "";
    [Key(1)]
    public string start_path { get; set; } = ".";

    public Nodetool.Types.FilePath Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.FilePath);
    }
}
