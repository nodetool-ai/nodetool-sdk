using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class PostRequestBinary
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public object data { get; set; } = "";

    public byte[] Process()
    {
        // Implementation would be generated based on node logic
        return default(byte[]);
    }
}
