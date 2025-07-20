using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class JSON
{
    [Key(0)]
    public Nodetool.Types.JSONRef value { get; set; } = new Nodetool.Types.JSONRef();

    public Nodetool.Types.JSONRef Process()
    {
        return default(Nodetool.Types.JSONRef);
    }
}
