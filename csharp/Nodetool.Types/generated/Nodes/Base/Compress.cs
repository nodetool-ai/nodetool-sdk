using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Compress
{
    [Key(0)]
    public byte[] data { get; set; }
    [Key(1)]
    public int level { get; set; } = 9;

    public byte[] Process()
    {
        return default(byte[]);
    }
}
