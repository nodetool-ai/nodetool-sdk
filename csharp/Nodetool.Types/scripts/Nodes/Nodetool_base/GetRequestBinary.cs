using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class GetRequestBinary
{
    [Key(0)]
    public string url { get; set; } = "";

    public byte[] Process()
    {
        // Implementation would be generated based on node logic
        return default(byte[]);
    }
}
