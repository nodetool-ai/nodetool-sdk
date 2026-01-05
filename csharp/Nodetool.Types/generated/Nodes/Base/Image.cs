using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Image
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef value { get; set; } = new Nodetool.Types.Core.ImageRef();

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
