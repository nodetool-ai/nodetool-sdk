using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Chunk
{
    [Key(0)]
    public int length { get; set; } = 100;
    [Key(1)]
    public int overlap { get; set; } = 0;
    [Key(2)]
    public string separator { get; set; } = null;
    [Key(3)]
    public string text { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
