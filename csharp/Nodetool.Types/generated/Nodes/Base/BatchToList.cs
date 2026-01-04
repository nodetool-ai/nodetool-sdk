using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class BatchToList
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef batch { get; set; } = new Nodetool.Types.Core.ImageRef();

    public object Process()
    {
        return default(object);
    }
}
