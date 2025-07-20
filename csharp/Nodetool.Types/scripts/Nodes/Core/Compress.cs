using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Compress
{
    [Key(0)]
    public byte[] data { get; set; }
    [Key(1)]
    public int level { get; set; } = 9;

    public byte[] Process()
    {
        // Implementation would be generated based on node logic
        return default(byte[]);
    }
}
