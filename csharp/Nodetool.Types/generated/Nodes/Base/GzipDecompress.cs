using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GzipDecompress
{
    [Key(0)]
    public object data { get; set; } = null;

    public byte[] Process()
    {
        return default(byte[]);
    }
}
