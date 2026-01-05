using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class JSON
{
    [Key(0)]
    public Nodetool.Types.Core.JSONRef value { get; set; } = new Nodetool.Types.Core.JSONRef();

    public Nodetool.Types.Core.JSONRef Process()
    {
        return default(Nodetool.Types.Core.JSONRef);
    }
}
