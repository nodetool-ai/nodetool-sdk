using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Now
{

    public Nodetool.Types.Datetime Process()
    {
        return default(Nodetool.Types.Datetime);
    }
}
