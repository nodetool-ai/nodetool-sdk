using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class GetFileSize
{
    [Key(0)]
    public Nodetool.Types.FilePath path { get; set; } = new Nodetool.Types.FilePath();

    public int Process()
    {
        // Implementation would be generated based on node logic
        return default(int);
    }
}
