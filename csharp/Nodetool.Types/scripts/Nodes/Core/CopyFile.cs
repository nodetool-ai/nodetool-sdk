using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class CopyFile
{
    [Key(0)]
    public Nodetool.Types.FilePath source_path { get; set; } = new Nodetool.Types.FilePath();
    [Key(1)]
    public Nodetool.Types.FilePath destination_path { get; set; } = new Nodetool.Types.FilePath();

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
