using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Equalize
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
