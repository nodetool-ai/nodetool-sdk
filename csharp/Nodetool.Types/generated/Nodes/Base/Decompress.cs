using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Decompress
{
    [Key(0)]
    public byte[] data { get; set; }

    public byte[] Process()
    {
        return default(byte[]);
    }
}
