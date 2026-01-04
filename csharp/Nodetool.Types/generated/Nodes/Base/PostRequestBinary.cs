using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class PostRequestBinary
{
    [Key(0)]
    public object data { get; set; } = @"";
    [Key(1)]
    public string url { get; set; } = @"";

    public byte[] Process()
    {
        return default(byte[]);
    }
}
