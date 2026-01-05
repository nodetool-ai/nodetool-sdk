using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RemoveOverlap
{
    [Key(0)]
    public object documents { get; set; } = new();
    [Key(1)]
    public int min_overlap_words { get; set; } = 2;

    public object Process()
    {
        return default(object);
    }
}
