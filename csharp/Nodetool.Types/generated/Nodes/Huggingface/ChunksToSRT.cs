using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ChunksToSRT
{
    [Key(0)]
    public List<Nodetool.Types.Core.AudioChunk> chunks { get; set; } = new();
    [Key(1)]
    public double time_offset { get; set; } = 0.0;
}
