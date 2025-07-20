using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class CreateDirectory
{
    [Key(0)]
    public Nodetool.Types.FilePath path { get; set; } = new Nodetool.Types.FilePath();
    [Key(1)]
    public bool exist_ok { get; set; } = true;

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
