using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ChunksToSRT
{
    [Key(0)]
    public object chunks { get; set; } = new List<object>();
    [Key(1)]
    public double time_offset { get; set; } = 0.0;

    public string Process()
    {
        return default(string);
    }
}
