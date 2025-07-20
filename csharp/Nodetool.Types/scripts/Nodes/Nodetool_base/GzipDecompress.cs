using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class GzipDecompress
{
    [Key(0)]
    public object data { get; set; } = null;

    public byte[] Process()
    {
        // Implementation would be generated based on node logic
        return default(byte[]);
    }
}
