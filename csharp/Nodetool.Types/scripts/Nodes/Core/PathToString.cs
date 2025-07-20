using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class PathToString
{
    [Key(0)]
    public Nodetool.Types.FilePath file_path { get; set; } = new Nodetool.Types.FilePath();

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
