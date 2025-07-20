using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Chunk
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public int length { get; set; } = 100;
    [Key(2)]
    public int overlap { get; set; } = 0;
    [Key(3)]
    public object separator { get; set; } = null;

    public object Process()
    {
        return default(object);
    }
}
