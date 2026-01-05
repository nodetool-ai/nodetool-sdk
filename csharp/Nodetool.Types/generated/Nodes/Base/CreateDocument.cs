using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CreateDocument
{

    public Nodetool.Types.Core.DocumentRef Process()
    {
        return default(Nodetool.Types.Core.DocumentRef);
    }
}
