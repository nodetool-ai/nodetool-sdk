using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Image
{
    [Key(0)]
    public Nodetool.Types.ImageRef value { get; set; } = new Nodetool.Types.ImageRef();

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
