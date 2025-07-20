using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class BatchToList
{
    [Key(0)]
    public Nodetool.Types.ImageRef batch { get; set; } = new Nodetool.Types.ImageRef();

    public object Process()
    {
        return default(object);
    }
}
