using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CreateIndexIVFFlat
{
    [Key(0)]
    public int dim { get; set; } = 768;
    [Key(1)]
    public object metric { get; set; } = @"L2";
    [Key(2)]
    public int nlist { get; set; } = 1024;

    public Nodetool.Types.Core.FaissIndex Process()
    {
        return default(Nodetool.Types.Core.FaissIndex);
    }
}
