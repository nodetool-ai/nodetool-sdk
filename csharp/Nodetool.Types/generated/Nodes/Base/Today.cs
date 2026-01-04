using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Today
{

    public Nodetool.Types.Core.Date Process()
    {
        return default(Nodetool.Types.Core.Date);
    }
}
