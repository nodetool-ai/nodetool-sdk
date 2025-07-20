using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Chroma;

[MessagePackObject]
public class RemoveOverlap
{
    [Key(0)]
    public object documents { get; set; } = new List<object>();
    [Key(1)]
    public int min_overlap_words { get; set; } = 2;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
