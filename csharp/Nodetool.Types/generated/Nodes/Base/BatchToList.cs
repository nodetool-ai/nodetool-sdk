using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Image;

[MessagePackObject]
public class BatchToList
{
    [Key(0)]
    public Nodetool.Types.ImageRef batch { get; set; } = new Nodetool.Types.ImageRef();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
