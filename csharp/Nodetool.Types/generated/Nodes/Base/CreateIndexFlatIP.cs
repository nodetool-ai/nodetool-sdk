using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CreateIndexFlatIP
{
    [Key(0)]
    public int dim { get; set; } = 768;

    public Nodetool.Types.Core.FaissIndex Process()
    {
        return default(Nodetool.Types.Core.FaissIndex);
    }
}
